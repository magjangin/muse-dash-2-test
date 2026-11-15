using System;
using System.Collections.Generic;
using MelonLoader;

namespace muse_dash_2_test
{
    /// <summary>
    /// BMS 특수 노트(롱노트 및 샌드백)의 분류 정의 열거형입니다.
    /// </summary>
    public enum BmsSpecialNoteType
    {
        Hold,    // 롱노트 (Hold)
        Sandbag  // 샌드백/연타 (Sandbag)
    }

    /// <summary>
    /// 매칭 완료된 특수 노트 쌍의 시간, 마디, 틱 등의 구조적 매핑 정보를 담고 있는 모델 클래스입니다.
    /// </summary>
    public sealed class BmsMatchedPair
    {
        public BmsSpecialNoteType Type { get; set; }
        public BmsNote StartNote { get; set; }
        public BmsNote EndNote { get; set; }

        // 시작과 끝의 시간 간격 (초 단위)
        public float Duration => EndNote.Time - StartNote.Time;

        // 시작과 끝의 틱(마디 비례 시간) 간격
        public float LengthInTicks => EndNote.Tick - StartNote.Tick;

        // 배치된 레인 (지상, 공중 등)
        public BmsLane Lane => StartNote.Lane;
    }

    /// <summary>
    /// BMS 차트의 파싱된 노트를 스캔하여 홀드 시작/끝, 샌드백 시작/끝을 
    /// 레인별로 추적해 한 쌍으로 안전하게 묶어주는 런타임 매칭 시스템 엔진 뼈대입니다.
    /// </summary>
    public static class BmsNoteMatcher
    {
        /// <summary>
        /// 파싱 완료된 BmsChart의 노트 리스트를 스캔하여 특수 노트 쌍을 매칭합니다.
        /// </summary>
        public static List<BmsMatchedPair> MatchSpecialNotes(IReadOnlyList<BmsNote> rawNotes, BmsChart chart)
        {
            var matchedPairs = new List<BmsMatchedPair>();
            if (rawNotes == null || rawNotes.Count == 0 || chart == null)
            {
                return matchedPairs;
            }

            // 1. 채널별로 노트를 임시 격리 분류합니다.
            var notesByChannel = new Dictionary<int, List<BmsNote>>();
            for (int i = 0; i < rawNotes.Count; i++)
            {
                var note = rawNotes[i];
                if (!notesByChannel.ContainsKey(note.Channel))
                {
                    notesByChannel[note.Channel] = new List<BmsNote>();
                }
                notesByChannel[note.Channel].Add(note);
            }

            // 2. 채널별로 틱 순서대로 순회하며 매칭을 가동합니다.
            foreach (var kvp in notesByChannel)
            {
                int channel = kvp.Key;
                // BmsParser가 이미 틱 순서로 노트를 돌려주고, 채널별 그룹화도 그 순서를 유지합니다.
                var sortedNotesInLane = kvp.Value;

                // 각 레인별 실시간 홀드/샌드백 매칭 상태 추적 슬롯
                BmsNote activeHoldStart = null;
                BmsNote activeSandbagStart = null;

                for (int i = 0; i < sortedNotesInLane.Count; i++)
                {
                    var currentNote = sortedNotesInLane[i];
                    
                    // WAV 매핑 데이터 조회
                    var wavInfo = BmsBossSwapPlanner.ResolveWavInfo(chart, currentNote);
                    if (wavInfo == null)
                    {
                        continue;
                    }

                    bool isHold = wavInfo.NoteType == 3;
                    bool isSandbag = wavInfo.NoteType == 8;

                    // ❶ 홀드(롱노트) 시작 및 끝 매칭 처리
                    if (isHold)
                    {
                        if (activeHoldStart == null)
                        {
                            // 홀드 시작 지점 지정
                            activeHoldStart = currentNote;
                            ModLogger.Verbose($"[BmsMatcher] 홀드 시작 등록 완료: Channel={channel}, Tick={currentNote.Tick}");
                        }
                        else
                        {
                            // 이미 대기 중인 시작점이 있으므로 현재 노트를 끝으로 매칭 완료
                            var pair = new BmsMatchedPair
                            {
                                Type = BmsSpecialNoteType.Hold,
                                StartNote = activeHoldStart,
                                EndNote = currentNote
                            };
                            matchedPairs.Add(pair);
                            ModLogger.Verbose($"[BmsMatcher] ★홀드 매칭 완성★ Channel={channel} | 시작={pair.StartNote.Tick:F3} 틱 ➡️ 끝={pair.EndNote.Tick:F3} 틱 (길이={pair.Duration:F2}초, {pair.LengthInTicks:F2}틱)");
                            
                            // 상태 초기화하여 다음 롱노트 매칭을 준비시킴
                            activeHoldStart = null;
                        }
                    }

                    // ❷ 샌드백(멀티히트) 시작 및 끝 매칭 처리
                    if (isSandbag)
                    {
                        if (activeSandbagStart == null)
                        {
                            // 샌드백 시작 지점 지정
                            activeSandbagStart = currentNote;
                            ModLogger.Verbose($"[BmsMatcher] 샌드백 시작 등록 완료: Channel={channel}, Tick={currentNote.Tick}");
                        }
                        else
                        {
                            // 샌드백 끝 노트로 매칭하여 길이 산출 완료
                            var pair = new BmsMatchedPair
                            {
                                Type = BmsSpecialNoteType.Sandbag,
                                StartNote = activeSandbagStart,
                                EndNote = currentNote
                            };
                            matchedPairs.Add(pair);
                            ModLogger.Verbose($"[BmsMatcher] ★샌드백 매칭 완성★ Channel={channel} | 시작={pair.StartNote.Tick:F3} 틱 ➡️ 끝={pair.EndNote.Tick:F3} 틱 (연타구간={pair.Duration:F2}초)");
                            
                            activeSandbagStart = null;
                        }
                    }
                }

                // 3. 루프 종료 후 짝을 찾지 못하고 남은 비정상 노드 경고 로깅 (차트 버그 진단용)
                if (activeHoldStart != null)
                {
                    ModLogger.Warning($"[BmsMatcher.Bug] 홀드 매칭 경고: 채널 {channel}의 Tick {activeHoldStart.Tick}에 선언된 롱노트의 짝(종료 노트)을 찾지 못했습니다.");
                }
                if (activeSandbagStart != null)
                {
                    ModLogger.Warning($"[BmsMatcher.Bug] 샌드백 매칭 경고: 채널 {channel}의 Tick {activeSandbagStart.Tick}에 선언된 샌드백의 짝(종료 노트)을 찾지 못했습니다.");
                }
            }

            return matchedPairs;
        }
    }
}
