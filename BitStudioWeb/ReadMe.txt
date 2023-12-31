  知之乐官网
  迁移命令
  需要在包管理器执行dotnet工具，防止ef命令找不到报错。
  dotnet tool install --global dotnet-ef --version 6.0.0
  然后下面的命令可以直接在包管理器执行，不需要命令行执行：

  先用cmd,执行切换到ef的项目文件下，执行
  cd BitStudioWeb
  再执行：
  dotnet ef migrations add InitialCreateXXXX
  dotnet ef database update
  
  执行生成sql命令
  dotnet ef migrations script -o sql.txt