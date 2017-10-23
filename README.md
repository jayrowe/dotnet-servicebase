# dotnet-servicebase

This package is designed to ease writing .NET Core applications that can run either as a console application or as a
Windows service. The basic idea is to try seeing if you can P/Invoke the proper Windows functions to run as a
Windows service, and if you can't fall back to another method.

Currently this will prefer to run as a Windows service and then fall back to a console application. This could be
extended further to support systemd's notifcation callback.

# Limitations

This is known to work on Windows, Ubuntu Linux and MacOS. Actual mileage may vary. It supports a single service per
process and allows start/stop callbacks. No pause or continue or anything like that, as I don't typically use those
anyway.

# Usage

To use this library:
* create a class from DotNetServiceBase.DotNetServiceBase.
* override the OnStart/OnStop methods as necessary
* create a new instance of your class and call Run()

The Run() method will block until the service is killed via an appropriate mechanism (service stop for Windows service,
CTRL-C/SIGHUP for console).
