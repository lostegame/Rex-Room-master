@echo off
set D=C:\Users\LOSTE GAME\Desktop\OpenRec-2-master
cd /d "%D%"
dotnet "bin\Release\net10.0\OpenRec-2.dll" >> "%D%\manboot_rec2_out.log" 2>&1