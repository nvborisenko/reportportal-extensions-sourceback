Install `ReportPortal.Extensions.SourceBack` nuget package into tests project and see in report where exactly test was failed.

![Example](docs/Screenshot.png)

# How it works
This package embedds your tests source code into `pdb` file. When test fails, `SourceBack` package tracks exceptions stacktrace and inserts corresponding piece of code. This works even if you build and execute tests on different machines.

# Requirements
You should compile test project with `portable` or `embedded` debug type option. This is already set for the most of projects by default. To change it: right click on the project in Solution Explorer window -> Properties -> Build -> Advanced -> Debugging Information.

# Configuration
In default `ReportPortal.config.json` file
```json
{
	"Extensions":{
		"SourceBack":
		{
			"OffsetUp": 4,
			"OffsetDown": 2
		}
	}
}
```
And alternative is via environment variables like `ReportPortal_Extensions_SourceBack_OffsetUp`.

# Known issues
Sometimes this extension doesn't highlight the line of code properly if project is built with Code Optimization enabled. Usually this option is disabled in Debug configuration (Right click on the project > Properties > Build > Disable `Optimize code`).