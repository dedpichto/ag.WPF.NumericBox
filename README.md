# ag.WPF.NumericBox

Custom WPF control for input and formatted output of decimal values

![ag.WPF.NumericBox](https://github.com/dedpichto/ag.WPF.NumericBox/raw/dev/ag.WPF.NumericBox/img/numericbox.png?raw=true "ag.WPF.NumericBox")

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

Property name | Return value | Description | Default value
--- | --- | --- | ---
DecimalPlaces | uint | Gets or sets the value that indicates the count of decimal digits shown at NumericBox | 0
IsReadOnly | bool | Gets or sets the value that indicates whether NumericBox is in read-only state | False
NegativeForeground | SolidColorBrush | Gets or sets the Brush to apply to the text contents of NumericBox when control's value is negative | Red
UseGroupSeparator | bool | Gets or sets the value that indicates whether group separator is used for number formatting | True
Value | decimal? | Gets or sets the value of NumericBox | null
TextAlignment | TextAlignment | Gets or sets the text alignment of NumericBox | Left

## Events

Event | Description
--- | ---
DecimalPlacesChanged |  Occurs when the DecimalPlaces property has been changed in some way
IsReadOnlyChanged | Occurs when the IsReadOnly property has been changed in some way
NegativeForegroundChanged | Occurs when the NegativeForeground property has been changed in some way
UseGroupSeparatorChanged | Occurs when the UseGroupSeparator property has been changed in some way
ValueChanged | Occurs when the Value property has been changed in some way
TextAlignmentChanged | Occurs when the TextAlignment property has been changed in some way
