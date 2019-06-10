# Notepad Editor UWP

This application is meant to serve as a remake of the first application I made with Universal Windows Platform: https://www.microsoft.com/en-us/p/universal-notepad-editor/9p7h6wpzdcrk?activetab=pivot:overviewtab

## Getting Started

### Prerequisites

Minimum version: Windows 10, Version 1809

Target Version: Windows 10, Version 1903

##### Nuget Packages Installed:

Win2D package is installed so that we're able to get the fonts as follows: 

```cs
string[] fonts = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
```

## Built With

* [Universal Windows Platform](https://developer.microsoft.com/en-us/windows/apps) - The desktop framework used

## Contributing

[Coming Soon]

## Authors

* **Hunter** - *Initial work* - [hjohnson012](https://github.com/hjohnson012)

See also the list of [contributors](https://github.com/hjohnson12/NotepadEditorUWP/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
