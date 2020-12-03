Install `ReportPortal.Extensions.SourceBack` nuget package into tests project and see in report where exactly test was failed.

![Example](docs/Screenshot.png)

# How it works
This package embedds your tests source code into `pdb` file. When test fails, `SourceBack` package tracks exceptions stacktrace and inserts corresponding piece of code. This works even if you build and execute tests on different machines.

# Requirements
You should compile test project with `portable` or `embedded` debug type option. This is already set for the most of projects by default. To change it: right click on the project in Solution Explorer window and then `Properties > Build > Advanced > Debugging Information`.

# Configuration
Configuration parameters are optional. Example for default `ReportPortal.config.json` file

```json
{
  "Extensions": {
    "SourceBack": {
        "OffsetUp": 4,
        "OffsetDown": 2
      }
   }
}
```

And alternative is via environment variables like `ReportPortal_Extensions_SourceBack_OffsetUp`. Read [more](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md) about it.

List of configuration parameters
| Name	| Description	|
| ---	| ---	|
| ReportPortal_Extensions_SourceBack_OffsetUp	| How many lines of code above to capture. Default: 4.	|
| ReportPortal_Extensions_SourceBack_OffsetDown	| How many lines of code below to capture. Default: 2.	|
| ReportPortal_Extensions_SourceBack_OpenWith	| Program where to open highlighted line of code. Currently only Visual Studio Code is supported. Default: vscode.	| 

# Troubleshooting

### Incorrect highlighed line in report

Sometimes this extension doesn't highlight the line of code properly if project is built with Code Optimization enabled. Usually this option is disabled in Debug configuration (Right click on the project `Properties > Build > Disable Optimize code`).

### Internal Tracing
If something doesn't work as you expect, please set environment variable `ReportPortal_TraceLevel=Verbose` to turn on internal report portal tracing. See verbosed messages in `ReportPortal.*.log` files.