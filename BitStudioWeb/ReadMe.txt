  知之乐官网
  迁移命令
  先用cmd,执行切换到ef的项目文件下，执行
  cd BitStudioWeb
  再执行：
  dotnet ef migrations add InitialCreateXXXX
  dotnet ef database update
  
  执行生成sql命令
  dotnet ef migrations script -o sql.txt