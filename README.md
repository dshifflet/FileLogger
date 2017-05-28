# Temporary File Logger
Log Temporary Files somewhere else via POST and give them a Time To Live (TTL)

## What This Does
* Service that allows you to POST files with a TTL.  After a certain time the files go away.
* Basic restful search
* Basic UI
* Client Code

## Getting Started
* Review the FileLoggerHost app.config there are several directories.  Make sure they are ok.
* Start the FileLoggerHost you can run it interactively or even do blah.exe -install and install it as a service.
* The host will be at _http://127.0.0.1/filelogger/ui/_ 
* Upload a file, be sure to fill out the minutes to live.
* To search for files go to _http://127.0.0.1/filelogger/ui/search.html_



