Installation steps:
  1 -> Extract files to a folder such as C:\Program Files\Plywood
  2 -> Add folder to server path environment variable
  3 -> Customise config in DefaultPullConfig.config
  4 -> Update config using "ply pull config load [Your customised config file]"
  5 -> ... go play

Usefull commands:
  ply pull all
    - this will synchronise all apps on the local machine
  ply push [COMMENT]
    - push the current directory as a new version with the given comment.