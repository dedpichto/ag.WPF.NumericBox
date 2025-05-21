using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ag.WPF.NumericBox
{
    /// <summary>
    /// Represents custom control for input and formatted output of decimal values
    /// </summary>
    /// 
    #region Named parts
    [TemplatePart(Name = _elementText, Type = typeof(TextBox))]
    #endregion

    public class NumericBox : Control
#nullable disable
    {
        private enum CurrentKey
        {
            None,
            Number,
            Delete,
            Back,
            Decimal
        }

        #region Constants
        private const string _elementText = "PART_Text";
        #endregion

        #region Elements
        private TextBox _textBox;
        #endregion

        private bool _gotFocus;
        private bool _userInput;
        private string _digit;
        private bool _isBackPressed;
        private bool _isDeletePressed;
        private bool _backAtEnd;
        private bool _deleteAtEnd;
        private bool _isLoaded;
        private SpecialCases _specialCases;

        #region Private collections
        private List<NumericBoxShortcut> _shortcuts { get; } = new List<NumericBoxShortcut>() {
            new() { Key = Key.D, Multiplier = 10 } ,
            new() { Key = Key.H, Multiplier = 100 } ,
            new() { Key = Key.K, Multiplier = 1000 } ,
            new() { Key = Key.L, Multiplier = 10000 } ,
            new() { Key = Key.C, Multiplier = 100000 } ,
            new() { Key = Key.M, Multiplier = 1000000 }
        };
        #endregion

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(NumericBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
        /// <summary>
        /// The identifier of the <see cref="DecimalPlaces"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(nameof(DecimalPlaces), typeof(uint), typeof(NumericBox),
                new FrameworkPropertyMetadata((uint)0, OnDecimalPlacesChanged));
        /// <summary>
        /// The identifier of the <see cref="UseGroupSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseGroupSeparatorProperty = DependencyProperty.Register(nameof(UseGroupSeparator), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnUseGroupSeparatorChanged));
        /// <summary>
        /// The identifier of the <see cref="NegativeForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeForegroundProperty = DependencyProperty.Register(nameof(NegativeForeground), typeof(SolidColorBrush), typeof(NumericBox),
                new FrameworkPropertyMetadata(Brushes.Red, OnNegativeForegroundChanged));
        /// <summary>
        /// The identifier of the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumericBox),
                new FrameworkPropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));
        /// <summary>
        /// The identifier of the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));
        /// <summary>
        /// The identifier of the <see cref="ShowTrailingZeros"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTrailingZerosProperty = DependencyProperty.Register(nameof(ShowTrailingZeros), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnShowTrailingZerosChanged));
        /// <summary>
        /// The identifier of the <see cref="TruncateFractionalPart"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TruncateFractionalPartProperty = DependencyProperty.Register(nameof(TruncateFractionalPart), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnTruncateFractionalPartChanged));
        /// <summary>
        /// The identifier of the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumericBox),
                new FrameworkPropertyMetadata("", OnTextChanged));
        /// <summary>
        /// The identifier of the <see cref="AllowShortcuts"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowShortcutsProperty = DependencyProperty.Register(nameof(AllowShortcuts), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(false, OnAllowShortcutsChanged));
        /// <summary>
        /// The identifier of the <see cref="ShortcutsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShortcutsSourceProperty = DependencyProperty.Register(nameof(ShortcutsSource), typeof(IEnumerable<NumericBoxShortcut>), typeof(NumericBox),
                new FrameworkPropertyMetadata(null));
        /// <summary>
        /// The identifier of the <see cref="DisableNegative"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisableNegativeProperty = DependencyProperty.Register(nameof(DisableNegative), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(false, OnDisableNegativeChanged));

        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the string representation of <see cref="Value"/> property.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                var tempValue = "";
                if (value != null)
                {
                    var groupsSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                    var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
                    while (value.StartsWith(groupsSeparator))
                        value = value.Substring(1);
                    while (value.EndsWith(groupsSeparator))
                        value = value.Substring(0, value.Length - 1);
                    value = value.Replace($"{groupsSeparator}{groupsSeparator}", groupsSeparator);
                    value = value.Replace($"{negativeSign}{groupsSeparator}", negativeSign);
                    tempValue = value.Replace($"{groupsSeparator}", "");
                }
                if (!string.IsNullOrEmpty(value) && !double.TryParse(tempValue, out var dv) && !value.In(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}"))
                {
                    if (dv > (double)decimal.MaxValue || dv < (double)decimal.MinValue)
                    {
                        Value = null;
                        SetValue(TextProperty, null);
                        return;
                    }
                    Value = null;
                    SetValue(TextProperty, null);
                    return;
                }
                SetValue(TextProperty, !string.IsNullOrEmpty(value) ? value : null);
            }
        }

        /// <summary>
        /// Gets or sets the property specified whether fractional part of decimal value will be truncated (True) accordingly to <see cref="DecimalPlaces"/> or rounded (False).
        /// </summary>
        public bool TruncateFractionalPart
        {
            get => (bool)GetValue(TruncateFractionalPartProperty);
            set => SetValue(TruncateFractionalPartProperty, value);
        }

        /// <summary>
        /// Gets or sets the text alignment of NumericBox.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the value of NumericBox.
        /// </summary>
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether group separator is used for number formatting.
        /// </summary>
        public bool UseGroupSeparator
        {
            get => (bool)GetValue(UseGroupSeparatorProperty);
            set => SetValue(UseGroupSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates the count of decimal digits shown at NumericBox.
        /// </summary>
        public uint DecimalPlaces
        {
            get => (uint)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        /// <summary>
        /// Gets or sets the Brush to apply to the text contents of NumericBox when control's value is negative.
        /// </summary>
        public SolidColorBrush NegativeForeground
        {
            get => (SolidColorBrush)GetValue(NegativeForegroundProperty);
            set => SetValue(NegativeForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether NumericBox is in read-only state.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether trailing zeroes in decimal part of NumericBox should be shown.
        /// </summary>
        public bool ShowTrailingZeros
        {
            get => (bool)GetValue(ShowTrailingZerosProperty);
            set => SetValue(ShowTrailingZerosProperty, value);
        }

        /// <summary>
        /// herGets or sets the value that indicates whet characters 'D', 'H', 'K', 'C', 'L', 'M' can be used for quick multiplying the <see cref="NumericBox.Value"/> by 10, 100, 1000, 10000, 100000 or 1000000.
        /// </summary>
        public bool AllowShortcuts
        {
            get => (bool)GetValue(AllowShortcutsProperty);
            set => SetValue(AllowShortcutsProperty, value);
        }

        /// <summary>
        /// Gets or sets the source of NumericBox shortcuts
        /// </summary>
        public IEnumerable<NumericBoxShortcut> ShortcutsSource
        {
            get => (IEnumerable<NumericBoxShortcut>)GetValue(ShortcutsSourceProperty);
            set => SetValue(ShortcutsSourceProperty, value);
        }
        /// <summary>
        /// Gets or sets the value that indicates whet negative input is enable
        /// </summary>
        public bool DisableNegative
        {
            get => (bool)GetValue(DisableNegativeProperty);
            set => SetValue(DisableNegativeProperty, value);
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// Invoked just before the <see cref="TextAlignmentChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextAlignmentChanged(TextAlignment oldValue, TextAlignment newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<TextAlignment>(oldValue, newValue)
            {
                RoutedEvent = TextAlignmentChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnTextAlignmentChanged((TextAlignment)(e.OldValue), (TextAlignment)(e.NewValue));
        }

        /// <summary>
        /// Invoked just before the <see cref="ValueChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnValueChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                if (((box._isDeletePressed && box._deleteAtEnd) || (box._isBackPressed && box._backAtEnd)) && !box.ShowTrailingZeros)
                    return;
                box.Text = box.convertToString((decimal?)e.NewValue);
            }
            box.OnValueChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        /// <summary>
        /// Invoked just before the <see cref="DecimalPlacesChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old decimal digits count</param>
        /// <param name="newValue">New decimal digits count</param>
        private void OnDecimalPlacesChanged(uint oldValue, uint newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<uint>(oldValue, newValue)
            {
                RoutedEvent = DecimalPlacesChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnDecimalPlacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                if (((box._isDeletePressed && box._deleteAtEnd) || (box._isBackPressed && box._backAtEnd)) && !box.ShowTrailingZeros)
                    return;
                box.Text = box.convertToString(box.Value);
            }
            box.OnDecimalPlacesChanged((uint)e.OldValue, (uint)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="UseGroupSeparatorChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnUseGroupSeparatorChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = UseGroupSeparatorChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnUseGroupSeparatorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                if (((box._isDeletePressed && box._deleteAtEnd) || (box._isBackPressed && box._backAtEnd)) && !box.ShowTrailingZeros)
                    return;
                box.Text = box.convertToString(box.Value);
            }
            box.OnUseGroupSeparatorChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="NegativeForegroundChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old foreground</param>
        /// <param name="newValue">New foreground</param>
        private void OnNegativeForegroundChanged(SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<SolidColorBrush>(oldValue, newValue)
            {
                RoutedEvent = NegativeForegroundChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnNegativeForegroundChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnNegativeForegroundChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="IsReadOnlyChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = IsReadOnlyChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnIsReadOnlyChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="ShowTrailingZerosChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnShowTrailingZerosChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = ShowTrailingZerosChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnShowTrailingZerosChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                if (((box._isDeletePressed && box._deleteAtEnd) || (box._isBackPressed && box._backAtEnd)) && !box.ShowTrailingZeros)
                    return;
                box.Text = box.convertToString(box.Value);
            }
            box.OnShowTrailingZerosChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="AllowShortcutsChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnAllowShortcutsChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = AllowShortcutsChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnAllowShortcutsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnAllowShortcutsChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="DisableNegativeChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnDisableNegativeChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = DisableNegativeEvent
            };
            RaiseEvent(e);
        }
        private static void OnDisableNegativeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnDisableNegativeChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="TextChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextChanged(string oldValue, string newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<string>(oldValue, newValue)
            {
                RoutedEvent = TextChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                var stringValue = (string)e.NewValue;
                if (box.DisableNegative && !string.IsNullOrEmpty(stringValue) && stringValue.StartsWith(CultureInfo.CurrentCulture.NumberFormat.NegativeSign))
                {
                    stringValue = stringValue.Substring(1);
                }
                if (box._textBox != null)
                    box._textBox.Text = stringValue;
                box.Value = box.convertToDecimal(stringValue);
            }
            box.OnTextChanged((string)e.OldValue, (string)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="TruncateFractionalPartChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTruncateFractionalPartChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = TruncateFractionalPartChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnTruncateFractionalPartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._isLoaded)
            {
                if (((box._isDeletePressed && box._deleteAtEnd) || (box._isBackPressed && box._backAtEnd)) && !box.ShowTrailingZeros)
                    return;
                box.Text = box.convertToString(box.Value);
            }
            box.OnTruncateFractionalPartChanged((bool)e.OldValue, (bool)e.NewValue);
        }
        #endregion

        #region Routed events
        /// <summary>
        /// Occurs when the <see cref="TextAlignment"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TextAlignment> TextAlignmentChanged
        {
            add => AddHandler(TextAlignmentChangedEvent, value);
            remove => RemoveHandler(TextAlignmentChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextAlignmentChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextAlignmentChangedEvent = EventManager.RegisterRoutedEvent("TextAlignmentChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<TextAlignment>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="Value"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="UseGroupSeparator"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> UseGroupSeparatorChanged
        {
            add => AddHandler(UseGroupSeparatorChangedEvent, value);
            remove => RemoveHandler(UseGroupSeparatorChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="UseGroupSeparatorChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent UseGroupSeparatorChangedEvent = EventManager.RegisterRoutedEvent("UseGroupSeparatorChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="DecimalPlaces"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<uint> DecimalPlacesChanged
        {
            add => AddHandler(DecimalPlacesChangedEvent, value);
            remove => RemoveHandler(DecimalPlacesChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="DecimalPlacesChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DecimalPlacesChangedEvent = EventManager.RegisterRoutedEvent("DecimalPlacesChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<uint>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="NegativeForeground"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<SolidColorBrush> NegativeForegroundChanged
        {
            add => AddHandler(NegativeForegroundChangedEvent, value);
            remove => RemoveHandler(NegativeForegroundChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="NegativeForegroundChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent NegativeForegroundChangedEvent = EventManager.RegisterRoutedEvent("NegativeForegroundChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<SolidColorBrush>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="IsReadOnly"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsReadOnlyChanged
        {
            add => AddHandler(IsReadOnlyChangedEvent, value);
            remove => RemoveHandler(IsReadOnlyChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="IsReadOnlyChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent IsReadOnlyChangedEvent = EventManager.RegisterRoutedEvent("IsReadOnlyChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="ShowTrailingZeros"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> ShowTrailingZerosChanged
        {
            add => AddHandler(ShowTrailingZerosChangedEvent, value);
            remove => RemoveHandler(ShowTrailingZerosChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ShowTrailingZerosChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ShowTrailingZerosChangedEvent = EventManager.RegisterRoutedEvent("ShowTrailingZerosChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="AllowShortcuts"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> AllowShortcutsChanged
        {
            add => AddHandler(AllowShortcutsChangedEvent, value);
            remove => RemoveHandler(AllowShortcutsChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="AllowShortcutsChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent AllowShortcutsChangedEvent = EventManager.RegisterRoutedEvent("AllowShortcutsChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="DisableNegative"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> DisableNegativeChanged
        {
            add => AddHandler(DisableNegativeEvent, value);
            remove => RemoveHandler(DisableNegativeEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="DisableNegativeChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DisableNegativeEvent = EventManager.RegisterRoutedEvent("DisableNegativeChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="TruncateFractionalPart"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> TruncateFractionalPartChanged
        {
            add => AddHandler(TruncateFractionalPartChangedEvent, value);
            remove => RemoveHandler(TruncateFractionalPartChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TruncateFractionalPartChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TruncateFractionalPartChangedEvent = EventManager.RegisterRoutedEvent("TruncateFractionalPartChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="Text"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<string> TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent("TextChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<string>), typeof(NumericBox));

        #endregion

        #region ctor
        static NumericBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericBox), new FrameworkPropertyMetadata(typeof(NumericBox)));
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Occurs when this System.Windows.FrameworkElement is initialized.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _isLoaded = true;
        }
        /// <summary>
        /// Is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var v = Value;
            if (_textBox != null)
            {
                _textBox.LostFocus -= textBox_LostFocus;
                _textBox.GotFocus -= textBox_GotFocus;
                _textBox.PreviewKeyDown -= textBox_PreviewKeyDown;
                _textBox.PreviewDrop -= _textBox_PreviewDrop;
                _textBox.PreviewDragEnter -= _textBox_PreviewDragEnter;
                _textBox.PreviewDragOver -= _textBox_PreviewDragOver;
                _textBox.TextChanged -= textBox_TextChanged;
                _textBox.PreviewMouseLeftButtonDown -= textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Clear();
                BindingOperations.ClearBinding(this, TextProperty);
            }
            _textBox = GetTemplateChild(_elementText) as TextBox;
            if (_textBox != null)
            {
                _textBox.LostFocus += textBox_LostFocus;
                _textBox.GotFocus += textBox_GotFocus;
                _textBox.PreviewKeyDown += textBox_PreviewKeyDown;
                _textBox.PreviewDrop += _textBox_PreviewDrop;
                _textBox.PreviewDragEnter += _textBox_PreviewDragEnter;
                _textBox.PreviewDragOver += _textBox_PreviewDragOver;
                _textBox.TextChanged += textBox_TextChanged;
                _textBox.PreviewMouseLeftButtonDown += textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, pasteCommandBinding));
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, cutCommandBinding));
                BindingOperations.SetBinding(this, TextProperty, new Binding("Text") { Source = _textBox, Mode = BindingMode.OneWay });
                _textBox.Text = convertToString(v);
            }
        }

        /// <summary>
        /// Called when the control receives keyboard focus.
        /// Ensures that the internal <see cref="TextBox"/> receives focus when the <see cref="NumericBox"/> is focused.
        /// </summary>
        /// <param name="e">The event data for the <see cref="RoutedEventArgs"/>.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            _textBox?.Focus();
        }
        #endregion

        #region Event handlers
        private void _textBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        private void _textBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        private void _textBox_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void textBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                _textBox.SelectAll();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = false;
            _userInput = false;
            _textBox.Text = convertToString(Value);
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = _textBox.Text;
            if (!ShowTrailingZeros)
            {
                if (_gotFocus)
                {
                    _textBox.SelectAll();
                    _gotFocus = false;
                }
            }
        }

        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _gotFocus = false;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.Left, Key.Right))
                    e.Handled = true;
                return;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.A, Key.C, Key.V, Key.X, Key.Z, Key.Y))
                    e.Handled = true;
                return;
            }
            else if (e.Key.In(Key.Left, Key.Right, Key.Home, Key.End, Key.Tab, Key.Enter, Key.Return))
            {
                return;
            }

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (DisableNegative)
            {
                if (e.Key.In(Key.OemMinus, Key.Subtract))
                {
                    e.Handled = true;
                    return;
                }
            }

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
            var isNegative = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) >= 0;
            var decimalIndex = _textBox.Text.IndexOf(decimalSeparator, StringComparison.Ordinal);
            var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
            var carIndex = _textBox.CaretIndex;
            var text = _textBox.Text;
            _digit = "";
            _isBackPressed = false;
            _isDeletePressed = false;
            _backAtEnd = false;
            _deleteAtEnd = false;

            if (isNegative && carIndex == 0 && !(_textBox.SelectionLength == _textBox.Text.Length))
            {
                if (!e.Key.In(Key.Delete, Key.Back))
                {
                    e.Handled = true;
                    return;
                }
            }
            switch (e.Key)
            {
                case Key.D0 or Key.NumPad0:
                    _digit = "0";
                    break;
                case Key.D1 or Key.NumPad1:
                    _digit = "1";
                    break;
                case Key.D2 or Key.NumPad2:
                    _digit = "2";
                    break;
                case Key.D3 or Key.NumPad3:
                    _digit = "3";
                    break;
                case Key.D4 or Key.NumPad4:
                    _digit = "4";
                    break;
                case Key.D5 or Key.NumPad5:
                    _digit = "5";
                    break;
                case Key.D6 or Key.NumPad6:
                    _digit = "6";
                    break;
                case Key.D7 or Key.NumPad7:
                    _digit = "7";
                    break;
                case Key.D8 or Key.NumPad8:
                    _digit = "8";
                    break;
                case Key.D9 or Key.NumPad9:
                    _digit = "9";
                    break;
                case Key.OemMinus or Key.Subtract:
                    if ((!isNegative && carIndex == 0) || (_textBox.SelectionLength == _textBox.Text.Length))
                    {
                        _userInput = true;
                        if (_textBox.SelectionLength == _textBox.Text.Length)
                        {
                            _textBox.Text = $"-";
                            _textBox.CaretIndex = 1;
                            e.Handled = true;
                            return;
                        }
                        _textBox.Text = $"-{text}";
                        _textBox.CaretIndex = 1;
                    }
                    e.Handled = true;
                    return;
                case Key.Delete:
                    _isDeletePressed = true;
                    if (carIndex == text.Length)
                    {
                        e.Handled = true;
                        return;
                    }
                    //full selection
                    if (_textBox.SelectionLength == text.Length)
                    {
                        _userInput = true;
                        Value = null;
                        e.Handled = true;
                        return;
                    }
                    //decimal part with caret index after decimal sign
                    if (decimalIndex >= 0 && carIndex > decimalIndex)
                    {
                        if (carIndex < text.Length)
                        {
                            _deleteAtEnd = true;
                            if (_textBox.SelectionLength == 0)
                            {
                                _userInput = true;
                                if (ShowTrailingZeros)
                                {
                                    _textBox.Text = text.SetChar('0', carIndex);
                                    _textBox.CaretIndex = carIndex + 1;
                                }
                                else
                                {
                                    _textBox.Text = _textBox.Text.Remove(carIndex, 1);
                                    _textBox.CaretIndex = carIndex;
                                }
                            }
                            else
                            {
                                _userInput = true;
                                var arr = new List<int>();
                                for (var i = _textBox.SelectionStart; ; i++)
                                {
                                    if (arr.Count == _textBox.SelectionLength)
                                        break;
                                    arr.Add(i);
                                }
                                if (ShowTrailingZeros)
                                {
                                    _textBox.Text = text.SetChars('0', arr.ToArray());
                                    _textBox.CaretIndex = carIndex + arr.Count;
                                }
                                else
                                {
                                    _textBox.Text = _textBox.Text.Remove(carIndex, _textBox.SelectionLength);
                                    _textBox.CaretIndex = carIndex;
                                }
                            }

                        }
                        e.Handled = true;
                        return;
                    }
                    if (_textBox.SelectionLength == 0)
                    {
                        if (decimalIndex >= 0 && carIndex == decimalIndex)
                        {
                            if (_textBox.Text == decimalSeparator)
                            {
                                _textBox.Text = null;
                                e.Handled = true;
                                return;
                            }
                            _textBox.CaretIndex++;
                            e.Handled = true;
                            return;
                        }
                        var before = text.Substring(0, carIndex);
                        _userInput = true;
                        if (text[carIndex] == groupSeparator[0])
                        {
                            _textBox.Text = text.Remove(carIndex, 2);
                        }
                        else
                        {
                            _textBox.Text = text.Remove(carIndex, 1);
                        }
                        var after = _textBox.Text.Length >= before.Length ? _textBox.Text.Substring(0, before.Length) : _textBox.Text;
                        var count1 = before.Count(c => c == groupSeparator[0]);
                        var count2 = after.Count(c => c == groupSeparator[0]);
                        _textBox.CaretIndex = carIndex + (count2 - count1);
                        e.Handled = true;
                        return;

                    }
                    else
                    {
                        var before = text.Substring(0, _textBox.SelectionStart);
                        _userInput = true;
                        _textBox.Text = text.Remove(_textBox.SelectionStart, _textBox.SelectionLength);
                        var after = _textBox.Text.Length >= before.Length ? _textBox.Text.Substring(0, before.Length) : _textBox.Text;
                        var count1 = before.Count(c => c == groupSeparator[0]);
                        var count2 = after.Count(c => c == groupSeparator[0]);
                        _textBox.CaretIndex = carIndex + (count2 - count1);
                        e.Handled = true;
                        return;
                    }
                case Key.Back:
                    _isBackPressed = true;
                    //full selection
                    if (_textBox.SelectionLength == text.Length)
                    {
                        _userInput = true;
                        Value = null;
                        e.Handled = true;
                        return;
                    }
                    if (carIndex == 0)
                    {
                        e.Handled = true;
                        return;
                    }
                    //decimal part with caret index after decimal sign
                    if (decimalIndex >= 0 && carIndex > decimalIndex)
                    {
                        _backAtEnd = true;
                        if (text[carIndex - 1] != decimalSeparator[0])
                        {
                            if (_textBox.SelectionLength == 0)
                            {
                                _userInput = true;
                                if (ShowTrailingZeros)
                                {
                                    _textBox.Text = text.SetChar('0', carIndex - 1);
                                }
                                else
                                {
                                    _textBox.Text = _textBox.Text.Remove(carIndex - 1, 1);
                                }
                                _textBox.CaretIndex = carIndex - 1;
                            }
                            else
                            {
                                var arr = new List<int>();
                                for (var i = _textBox.SelectionStart; ; i++)
                                {
                                    if (arr.Count == _textBox.SelectionLength)
                                        break;
                                    arr.Add(i);
                                }
                                _userInput = true;
                                if (ShowTrailingZeros)
                                {
                                    _textBox.Text = text.SetChars('0', arr.ToArray());
                                }
                                else
                                {
                                    _textBox.Text = _textBox.Text.Remove(carIndex, _textBox.SelectionLength);
                                }
                                _textBox.CaretIndex = carIndex;
                            }

                        }
                        else
                        {
                            if (_textBox.SelectionLength == 0)
                            {
                                if (_textBox.Text == decimalSeparator)
                                {
                                    _textBox.Text = null;
                                    e.Handled = true;
                                    return;
                                }
                                _textBox.CaretIndex = carIndex - 1;
                            }
                            else
                            {
                                var arr = new List<int>();
                                for (var i = _textBox.SelectionStart; ; i++)
                                {
                                    if (arr.Count == _textBox.SelectionLength)
                                        break;
                                    arr.Add(i);
                                }
                                _userInput = true;
                                if (ShowTrailingZeros)
                                {
                                    _textBox.Text = text.SetChars('0', arr.ToArray());
                                }
                                else
                                {
                                    _textBox.Text = _textBox.Text.Remove(carIndex, _textBox.SelectionLength);
                                }
                                _textBox.CaretIndex = carIndex;
                            }
                        }
                        e.Handled = true;
                        return;
                    }
                    if (_textBox.SelectionLength == 0)
                    {
                        var before = text.Substring(0, carIndex);
                        _userInput = true;
                        var isGroup = false;
                        if (text[carIndex - 1] == groupSeparator[0])
                        {
                            isGroup = true;
                            _textBox.Text = text.Remove(carIndex - 1, 2);
                        }
                        else
                        {
                            _textBox.Text = text.Remove(carIndex - 1, 1);
                        }
                        var after = _textBox.Text.Length >= before.Length ? _textBox.Text.Substring(0, before.Length) : _textBox.Text;
                        var count1 = before.Count(c => c == groupSeparator[0]);
                        var count2 = after.Count(c => c == groupSeparator[0]);
                        var diff = count2 - count1;
                        if (diff < 0)
                            diff = 0;
                        _textBox.CaretIndex = carIndex + diff - (isGroup ? 2 : 1);
                        e.Handled = true;
                        return;

                    }
                    else
                    {
                        var before = text.Substring(0, _textBox.SelectionStart);
                        _userInput = true;
                        _textBox.Text = text.Remove(_textBox.SelectionStart, _textBox.SelectionLength);
                        var after = _textBox.Text.Length >= before.Length ? _textBox.Text.Substring(0, before.Length) : _textBox.Text;
                        var count1 = before.Count(c => c == groupSeparator[0]);
                        var count2 = after.Count(c => c == groupSeparator[0]);
                        var diff = count2 - count1;
                        if (diff < 0)
                            diff = 0;
                        _textBox.CaretIndex = carIndex + diff;
                        e.Handled = true;
                        return;
                    }
                case Key.OemPeriod or Key.Decimal:
                    if (decimalSeparator == ".")
                    {
                        if (DecimalPlaces == 0)
                        {
                            e.Handled = true;
                        }
                        else if (decimalIndex >= 0 && _textBox.SelectionLength != text.Length)
                        {
                            _textBox.CaretIndex = decimalIndex + 1;
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        e.Handled = true;
                    }
                    break;
                case Key.OemComma:
                    if (decimalSeparator == ",")
                    {
                        if (DecimalPlaces == 0)
                        {
                            e.Handled = true;
                        }
                        else if (decimalIndex >= 0 && _textBox.SelectionLength != text.Length)
                        {
                            _textBox.CaretIndex = decimalIndex + 1;
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        e.Handled = true;
                    }
                    break;
                default:
                    if (AllowShortcuts && Value != null)
                    {
                        var shortcuts = getShortcuts();
                        var shortcut = shortcuts.FirstOrDefault(sc => sc.Key == e.Key);
                        if (shortcut != null)
                        {
                            Value *= shortcut.Multiplier;
                            _textBox.CaretIndex = carIndex;
                        }
                    }
                    e.Handled = true;
                    break;
            }

            if (string.IsNullOrEmpty(_digit))
                return;

            _userInput = true;
            var isCaretAtEnd = carIndex == _textBox.Text.Length && carIndex > 0;
            var selectionStart = _textBox.SelectionStart;
            var beforeDigit = text.Substring(0, _textBox.SelectionStart);
            if (_textBox.SelectionLength > 0)
            {
                _textBox.SelectedText = _digit;
            }
            else
            {
                if (decimalIndex >= 0)
                {
                    if (carIndex > decimalIndex)
                    {
                        var textBefore = _textBox.Text.Substring(decimalIndex + 1, carIndex - (decimalIndex + 1));
                        var textAfter = $"{_digit}{_textBox.Text.Substring(carIndex)}";
                        var textTotal = $"{textBefore}{textAfter}";
                        if (textTotal.Length > DecimalPlaces)
                        {
                            var temp = _textBox.Text.Insert(carIndex, _digit);
                            _textBox.Text = temp.Substring(0, temp.Length - 1);
                        }
                        else
                        {
                            _textBox.Text = _textBox.Text.Insert(carIndex, _digit);
                        }
                    }
                    else
                    {
                        _textBox.Text = _textBox.Text.Insert(carIndex, _digit);
                    }
                }
                else
                {
                    var textBefore = _textBox.Text;
                    _textBox.Text = _textBox.Text.Insert(carIndex, _digit);
                    var textAfter = _textBox.Text;
                    if (isCaretAtEnd && textAfter.Length > textBefore.Length)
                        isCaretAtEnd = false;
                }

            }
            if (isCaretAtEnd)
            {
                _textBox.CaretIndex = _textBox.Text.Length;
            }
            else if (carIndex < _textBox.Text.Length)
            {
                var afterDigit = _textBox.Text.Substring(0, selectionStart + _digit.Length);
                var count1Digit = beforeDigit.Count(c => c == groupSeparator[0]);
                var count2Digit = afterDigit.Count(c => c == groupSeparator[0]);
                _textBox.CaretIndex = carIndex + (count2Digit - count1Digit) + _digit.Length;
            }
            else
            {
                _textBox.CaretIndex = _textBox.Text.Length;
            }
            e.Handled = true;
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = true;
            _textBox.SelectAll();
        }
        #endregion

        #region Private enums
        private enum SpecialCases
        {
            None,
            Negative,
            NegativeZero,
            NegativeDot,
            Dot,
            EndDot,
            StartDot,
            NegativeDotNumber,
            NegativeZeroDot,
            NegativeZeroDotNumber
        }
        #endregion

        #region Private procedures
        private IEnumerable<NumericBoxShortcut> getShortcuts()
        {
            var b = BindingOperations.GetBinding(this, ShortcutsSourceProperty);
            if (b == null)
            {
                return _shortcuts;
            }
            else
            {
                var hashSet = new HashSet<NumericBoxShortcut>(ShortcutsSource);
                foreach (var sc in _shortcuts)
                    hashSet.Add(sc);
                return hashSet;
            }
        }

        private string getRealFractionString(decimal value, CultureInfo culture)
        {
            var arr = value.ToString().Split(culture.NumberFormat.NumberDecimalSeparator[0]);
            if (arr.Length == 2)
                return arr[1];
            return null;
        }

        private (string, string) getDecimalParts(decimal value, CultureInfo culture)
        {
            var arr = value.ToString().Split(culture.NumberFormat.NumberDecimalSeparator[0]);
            var stringInt = arr[0];
            var stringFraction = "";
            if (arr.Length == 2)
            {
                stringFraction = arr[1];
            }
            if (stringFraction.Length < DecimalPlaces && ShowTrailingZeros)
            {
                stringFraction = stringFraction.PadRight((int)DecimalPlaces, '0');
            }
            return (stringInt, stringFraction);
        }

        private decimal? getDecimalFromString(string stringValue)
        {
            if (double.TryParse(stringValue, out double doubleValue))
            {
                if (doubleValue <= (double)decimal.MaxValue && doubleValue >= (double)decimal.MinValue)
                    return decimal.Parse(stringValue, NumberStyles.Any);
                else if (doubleValue > (double)decimal.MaxValue)
                    return decimal.MaxValue;
                else
                    return decimal.MinValue;
            }
            return null;
        }

        private string convertToString(decimal? decimalValue)
        {
            try
            {
                var culture = CultureInfo.CurrentCulture;
                var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
                var negativeSign = culture.NumberFormat.NegativeSign;
                var groupSeparator = culture.NumberFormat.NumberGroupSeparator;

                if (decimalValue == null)
                {
                    if (_specialCases == SpecialCases.Negative && _userInput)
                    {
                        return negativeSign;
                    }
                    else if (_specialCases == SpecialCases.NegativeZero && _userInput)
                    {
                        return $"{negativeSign}0";
                    }
                    else if (_specialCases == SpecialCases.NegativeDot && _userInput)
                    {
                        return $"{negativeSign}{decimalSeparator}";
                    }
                    else if (_specialCases == SpecialCases.Dot && _userInput)
                    {
                        return decimalSeparator;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (DisableNegative && decimalValue < 0)
                {
                    decimalValue = Math.Abs(decimalValue.Value);
                }

                var isFocused = _textBox != null && _textBox.IsFocused;

                var formatInt = UseGroupSeparator ? "#" + groupSeparator + "##0" : "##0";
                var formatFraction = new string('0', (int)DecimalPlaces);

                var (intpPart, fracPart) = getDecimalParts(decimalValue.Value, culture);
                if (_specialCases == SpecialCases.NegativeZeroDot && _userInput)
                {
                    return $"{negativeSign}0{decimalSeparator}";
                }
                if (_specialCases == SpecialCases.NegativeDotNumber && _userInput)
                {
                    if (fracPart.Length > DecimalPlaces)
                    {
                        if (TruncateFractionalPart)
                            fracPart = fracPart.Substring(0, (int)DecimalPlaces);
                        else
                            fracPart = Convert.ToDecimal($"0{decimalSeparator}{fracPart}").ToString($"0{decimalSeparator}{formatFraction}").Substring(2);
                    }
                    return $"{negativeSign}{decimalSeparator}{fracPart}";
                }
                if (_specialCases == SpecialCases.NegativeZeroDotNumber && _userInput)
                {
                    if (fracPart.Length > DecimalPlaces)
                    {
                        if (TruncateFractionalPart)
                            fracPart = fracPart.Substring(0, (int)DecimalPlaces);
                        else
                            fracPart = Convert.ToDecimal($"0{decimalSeparator}{fracPart}").ToString($"0{decimalSeparator}{formatFraction}").Substring(2);
                    }
                    return $"{negativeSign}0{decimalSeparator}{fracPart}";
                }
                if (_specialCases == SpecialCases.EndDot)
                {
                    intpPart = Convert.ToInt64(intpPart).ToString(formatInt);
                    if (_userInput)
                    {
                        return $"{intpPart}{decimalSeparator}";
                    }
                    else
                    {
                        if (!ShowTrailingZeros)
                        {
                            return intpPart;
                        }
                        else
                        {
                            var format = $"{formatInt}{decimalSeparator}{formatFraction}";
                            return decimalValue.Value.ToString(format);
                        }
                    }
                }
                if (_specialCases == SpecialCases.StartDot && _userInput)
                {
                    if (fracPart.Length > DecimalPlaces)
                    {
                        if (TruncateFractionalPart)
                            fracPart = fracPart.Substring(0, (int)DecimalPlaces);
                        else
                            fracPart = Convert.ToDecimal($"0{decimalSeparator}{fracPart}").ToString($"0{decimalSeparator}{formatFraction}").Substring(2);
                    }
                    return $"{decimalSeparator}{fracPart}";
                }

                string result;
                if (TruncateFractionalPart)
                {
                    if (DecimalPlaces > 0)
                    {
                        if (fracPart.Length > DecimalPlaces)
                            fracPart = fracPart.Substring(0, (int)DecimalPlaces);
                        if (intpPart != "-0")
                            intpPart = Convert.ToInt64(intpPart).ToString(formatInt);
                        if (!ShowTrailingZeros && !_userInput)
                        {
                            fracPart = fracPart.TrimEnd('0');
                        }
                        result = $"{intpPart}{decimalSeparator}{fracPart}";
                    }
                    else
                    {
                        if (intpPart != "-0")
                            result = Convert.ToInt64(intpPart).ToString(formatInt);
                        else
                            result = "-0";
                    }
                }
                else
                {
                    if (DecimalPlaces > 0)
                    {
                        var format = $"{formatInt}{decimalSeparator}{formatFraction}";
                        result = decimalValue.Value.ToString(format);
                        if (!ShowTrailingZeros)
                        {
                            if (!_userInput)
                            {
                                result = result.TrimEnd('0');
                            }
                            else
                            {
                                if (!_digit.In("", "0") || _isBackPressed || _isDeletePressed)
                                {
                                    result = result.TrimEnd('0');
                                }
                            }
                        }
                    }
                    else
                    {
                        if (intpPart != "-0")
                            result = Convert.ToInt64(intpPart).ToString(formatInt);
                        else
                            result = "-0";
                    }
                }
                if (!_userInput)
                {
                    result = result.TrimEnd(decimalSeparator[0]);
                }
                else
                {
                    if (_specialCases != SpecialCases.EndDot)
                    {
                        result = result.TrimEnd(decimalSeparator[0]);
                    }
                }
                return result;
            }
            finally
            {
                _userInput = false;
            }
        }

        private decimal? convertToDecimal(string stringValue)
        {
            _specialCases = SpecialCases.None;
            var culture = CultureInfo.CurrentCulture;
            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
            var negativeSign = culture.NumberFormat.NegativeSign;
            var groupSeparator = culture.NumberFormat.NumberGroupSeparator;

            if (!string.IsNullOrEmpty(stringValue))
                stringValue = stringValue.Replace(groupSeparator, "");
            else
                return null;

            decimal? result = null;

            if (stringValue == negativeSign)
            {
                _specialCases = SpecialCases.Negative;
                return null;
            }
            else if (stringValue == $"{negativeSign}{decimalSeparator}")
            {
                _specialCases = SpecialCases.NegativeDot;
                return null;
            }
            else if (stringValue == decimalSeparator)
            {
                _specialCases = SpecialCases.Dot;
                return null;
            }
            else if (stringValue == $"{negativeSign}0")
            {
                _specialCases = SpecialCases.NegativeZero;
                return null;
            }
            else if (stringValue.EndsWith(decimalSeparator))
            {
                _specialCases = SpecialCases.EndDot;
                result = getDecimalFromString(stringValue.Substring(0, stringValue.Length - 1));
                if (stringValue.StartsWith($"{negativeSign}0{decimalSeparator}"))
                {
                    _specialCases = SpecialCases.NegativeZeroDot;
                }
            }
            else if (stringValue.StartsWith(decimalSeparator))
            {
                _specialCases = SpecialCases.StartDot;
                result = getDecimalFromString(stringValue);
            }
            else if (stringValue.StartsWith($"{negativeSign}0{decimalSeparator}"))
            {
                if (stringValue == $"{negativeSign}0{decimalSeparator}")
                {
                    _specialCases = SpecialCases.NegativeDotNumber;
                    result = 0;
                }
                else
                {
                    var arr = stringValue.Split(decimalSeparator[0]);
                    if (arr.Length == 2 && arr[1].All(c => c == '0'))
                    {
                        _specialCases = SpecialCases.NegativeDotNumber;
                        result = 0;
                    }
                    else
                    {
                        if (stringValue.Length > $"{negativeSign}0{decimalSeparator}".Length)
                            _specialCases = SpecialCases.NegativeZeroDotNumber;
                        else
                            _specialCases = SpecialCases.NegativeDotNumber;
                        result = getDecimalFromString(stringValue);
                    }
                }
            }
            else if (stringValue.StartsWith($"{negativeSign}{decimalSeparator}"))
            {
                if (stringValue == $"{negativeSign}{decimalSeparator}")
                {
                    _specialCases = SpecialCases.NegativeDot;
                    return null;
                }
                else
                {
                    var arr = stringValue.Split(decimalSeparator[0]);
                    if (arr.Length == 2 && arr[1].All(c => c == '0'))
                    {
                        result = 0;
                    }
                    else
                    {
                        _specialCases = SpecialCases.NegativeDotNumber;
                        result = getDecimalFromString(stringValue);
                    }
                }
            }
            else
            {
                result = getDecimalFromString(stringValue);
            }

            return result;
        }

        private void cutCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            Clipboard.SetText(_textBox.SelectedText);
            if (_textBox.SelectionLength != _textBox.Text.Length)
            {
                var carIndex = _textBox.CaretIndex;
                var groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                var text = _textBox.Text;
                var before = text.Substring(0, _textBox.SelectionStart);
                _textBox.Text = text.Remove(_textBox.SelectionStart, _textBox.SelectionLength);
                var after = _textBox.Text.Length >= before.Length ? _textBox.Text.Substring(0, before.Length) : _textBox.Text;
                var count1 = before.Count(c => c == groupSeparator[0]);
                var count2 = after.Count(c => c == groupSeparator[0]);
                _textBox.CaretIndex = carIndex + (count2 - count1);
            }
            else
            {
                Value = null;
            }
        }

        private void pasteCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (Clipboard.ContainsText())
            {
                var clipboardText = Clipboard.GetText();

                if (!decimal.TryParse(clipboardText, out _))
                {
                    e.Handled = true;
                }
                else
                {
                    if (DisableNegative && !string.IsNullOrEmpty(clipboardText) && clipboardText.StartsWith(CultureInfo.CurrentCulture.NumberFormat.NegativeSign))
                    {
                        clipboardText = clipboardText.Substring(1);
                    }
                    var carIndex = _textBox.CaretIndex;
                    var selectionStart = _textBox.SelectionStart;
                    var groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                    var text = _textBox.Text;
                    var before = text.Substring(0, _textBox.SelectionStart);
                    if (_textBox.SelectionLength > 0)
                        _textBox.SelectedText = clipboardText;
                    else
                        _textBox.Text = _textBox.Text.Insert(_textBox.CaretIndex, clipboardText);
                    var after = _textBox.Text.Length >= carIndex + clipboardText.Length
                        ? _textBox.Text.Substring(0, carIndex + clipboardText.Length)
                        : _textBox.Text;

                    var count1 = before.Count(c => c == groupSeparator[0]);
                    var count2 = after.Count(c => c == groupSeparator[0]);
                    _textBox.CaretIndex = carIndex + (count2 - count1) + clipboardText.Length;
                }
            }
            else
            {
                e.Handled = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents the object for setting key/multiplier pair. When the specified key is pressed, the <see cref="NumericBox.Value"/> is multiplied by corresponding multiplier.
    /// </summary>
    public class NumericBoxShortcut
    {
        /// <summary>
        /// Gets or sets the shortcut's multiplier
        /// </summary>
        public int Multiplier { get; set; }
        /// <summary>
        /// Gets or sets the shortcut's key
        /// </summary>
        public Key Key { get; set; }
        /// <inheritdoc/>
        public override int GetHashCode() => Key.GetHashCode();
    }

    internal static class Extensions
    {
        internal static bool In<T>(this T obj, params T[] values) => values.Contains(obj);

        internal static string SetChar(this string str, char c, int index)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var arr = str.ToCharArray();
            arr[index] = c;
            return new string(arr);
        }

        internal static string SetChars(this string str, char c, params int[] indexes)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var arr = str.ToCharArray();
            foreach (var i in indexes)
                arr[i] = c;
            return new string(arr);
        }
    }

    /// <summary>
    /// Represents class for NumericBox foreground conversion.
    /// </summary>
    public class NumericBoxForegroundConverter : IMultiValueConverter
    {
        /// <summary>
        /// Determines NumericBox foreground.
        /// </summary>
        /// <param name="values">Array consists of current NumericBox value, regular foreground brush and negative foreground brush</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Brush depended on current value sign.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is not Brush foregroundBrush || values[2] is not Brush negativeBrush) return null;
            if (values[0] is not decimal decimalValue)
                return foregroundBrush;
            return decimalValue >= 0 ? foregroundBrush : negativeBrush;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Not used.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
