# ag.WPF.NumericBox

Custom WPF control for input and formatted output of decimal values

![Nuget](https://img.shields.io/nuget/v/ag.WPF.NumericBox)

![ag.WPF.NumericBox](https://am3pap005files.storage.live.com/y4mhS74mqoGJrNoAnjaOXlLKejHz1kagtJDHhYQIVP8Yfq2X-TqbYdnvRdcoD0womRo4hLLc3uoFxFIPYm7G09GS4V4QkYK73V0UMg9PbHeioll1SmKOf-178UOx7q1QxvsxrHeyuvnE7E45UKgE3QCEq65IIJt5wBJM_6g-MTT7KcidkzfL4vEzkBARYwEfIVX?width=274&height=83&cropmode=none "ag.WPF.NumericBox")

## Installation

Use Nuget packages

[ag.WPF.NumericBox](https://www.nuget.org/packages/ag.WPF.NumericBox/)

## Usage

```csharp
<Window x:Class="TestNet.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:num="clr-namespace:ag.WPF.NumericBox;assembly=ag.WPF.NumericBox"
        xmlns:local="clr-namespace:TestNet"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <num:NumericBox Width="400" DecimalPlaces="2" Value="0" />
    </Grid>
</Window>
```

## Properties

Property name | Value type | Description | Default value
--- | --- | --- | ---
DecimalPlaces | uint | Gets or sets the value that indicates the count of decimal digits shown at NumericBox | 0
IsReadOnly | bool | Gets or sets the value that indicates whether NumericBox is in read-only state | False
NegativeForeground | SolidColorBrush | Gets or sets the Brush to apply to the text contents of NumericBox when control's value is negative | Red
UseGroupSeparator | bool | Gets or sets the value that indicates whether group separator is used for number formatting | True
Value | decimal? | Gets or sets the value of NumericBox | null
TextAlignment | TextAlignment | Gets or sets the text alignment of NumericBox | Left
ShowTrailingZeros | bool | Gets or sets the value that indicates whether trailing zeros in decimal part of NumericBox should be shown | True
TruncateFractionalPart | bool | Gets or sets the property specified whether fractional part of decimal value will be truncated (True) accordingly to DecimalPlaces or rounded (False) | True
Text | string | Gets or sets the string representation of Value property | Empty string
AllowShortcuts | bool | Gets or sets the value that indicates whether characters 'D', 'H', 'K', 'C', 'L', 'M' can be used for quick multiplying the NumericBox value by 10, 100, 1000, 10000, 100000 or 1000000. | False
ShortcutsSource | IEnumerable\<NumericBoxShortcut\> | Gets or sets the source of NumericBox shortcuts | null

## Remarks

**AllowShortcuts** property defaults are:

- D =\> multiplies the value by 10
- H =\> multiplies the value by 100
- K =\> multiplies the value by 1000
- L =\> multiplies the value by 10000
- C =\> multiplies the value by 100000
- M =\> multiplies the value by 1000000

Setting the **ShortcutsSource** property will add new shortcusts or override existing ones (depending by shortcut key).

## Additional classes

### NumericBoxShortcut

Represents the object for setting key/multiplier pair. When the specified key is pressed, the value of NumericBox is multiplied by corresponding multiplier.

Property name | Value type | Description
--- | --- | ---
Multiplier | int | Gets or sets the shortcut's multiplier
Key | System.Windows.Input.Key | Gets or sets the shortcut's key

## Events

Event | Description
--- | ---
DecimalPlacesChanged |  Occurs when the DecimalPlaces property has been changed in some way
IsReadOnlyChanged | Occurs when the IsReadOnly property has been changed in some way
NegativeForegroundChanged | Occurs when the NegativeForeground property has been changed in some way
UseGroupSeparatorChanged | Occurs when the UseGroupSeparator property has been changed in some way
ValueChanged | Occurs when the Value property has been changed in some way
TextAlignmentChanged | Occurs when the TextAlignment property has been changed in some way
ShowTrailingZerosChanged | Occurs when the ShowTrailingZeros property has been changed in some way
TruncateFractionalPartChanged | Occurs when the TruncateFractionalPart property has been changed in some way
