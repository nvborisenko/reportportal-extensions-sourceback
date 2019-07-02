Install `ReportPortal.Extensions.SourceBack` nuget package into tests project and see in report where exactly test was failed.

![Example](docs/Screenshot.png)

# How it works
This package embedds your tests source code into `pdb` file. When test fails, `sourceback` package tracks exceptions stacktrace and inserts corresponding piece of code. This works even if you build and execute tests on different machines.

# Requirements
You should compile test project with `portable` or `embedded` debug type option. This is already set for the most of projects by default. To change it: right click on the project in Solution Explorer window -> Properties -> Build -> Advanced -> Debugging Information.
