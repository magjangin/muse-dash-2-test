Get-Content 'search_members_output.txt' | Where-Object { $_ -match '^Type:' }
