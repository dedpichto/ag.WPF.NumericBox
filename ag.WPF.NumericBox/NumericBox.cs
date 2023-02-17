using System;
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
    [TemplatePart(Name = ElementText, Type = typeof(TextBox))]
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

        private struct CurrentPosition
        {
            public CurrentKey Key;
            public int Offset;
            public bool Exclude;
        }

        #region Constants
        private const string ElementText = "PART_Text";
        #endregion

        #region Elements
        private TextBox _textBox;
        #endregion

        private CurrentPosition _Position;

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(NumericBox),
                new FrameworkPropertyMetadata(null, OnValueChanged));
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
        #endregion

        #region Public properties
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
        #endregion

        #region ctor
        static NumericBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericBox), new FrameworkPropertyMetadata(typeof(NumericBox)));
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_textBox != null)
            {
                _textBox.LostFocus -= TextBox_LostFocus;
                _textBox.GotFocus -= TextBox_GotFocus;
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                _textBox.TextChanged -= TextBox_TextChanged;
                _textBox.CommandBindings.Clear();
            }
            _textBox = GetTemplateChild(ElementText) as TextBox;
            if (_textBox != null)
            {
                _textBox.LostFocus += TextBox_LostFocus;
                _textBox.GotFocus += TextBox_GotFocus;
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.PreviewTextInput += TextBox_PreviewTextInput;
                _textBox.TextChanged += TextBox_TextChanged;
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, pasteCommandBinding));
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, cutCommandBinding));
            }
        }
        #endregion

        #region Event handlers
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox.Text == "-")
            {
                Value = null;
                BindingOperations.GetBindingExpression(_textBox, TextBox.TextProperty).UpdateSource();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_Position.Exclude)
                return;
            if (_Position.Key.In(CurrentKey.Number, CurrentKey.Back, CurrentKey.Decimal))
            {
                _textBox.CaretIndex = _textBox.Text.Length - _Position.Offset;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator)
                {
                    e.Handled = true;
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                {
                    if (DecimalPlaces == 0)
                    {
                        e.Handled = true;
                        return;
                    }

                    if (_textBox.Text != "-")
                    {
                        _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                        e.Handled = true;
                    }
                    else
                    {
                        _Position.Key = CurrentKey.Decimal;
                    }
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                {
                    if (_textBox.SelectionLength == _textBox.Text.Length)
                    {
                        return;
                    }
                    if (_textBox.CaretIndex > 0)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else if (!e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
                {
                    e.Handled = true;
                    return;
                }

                if (e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9")
                    && _textBox.Text.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    && _textBox.CaretIndex == _textBox.Text.Length)
                {
                    e.Handled = true;
                    return;
                }
                else
                {
                    _Position.Key = CurrentKey.Number;
                }
            }
            finally
            {
                if (!e.Handled)
                {
                    setPositionOffset();
                }
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _Position.Key = CurrentKey.None;
            _Position.Offset = 0;
            _Position.Exclude = false;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (e.Key != Key.Home && e.Key != Key.End)
                    e.Handled = true;
                return;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.A, Key.C, Key.V, Key.X, Key.Z, Key.Y))
                    e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    break;
                case Key.Delete:
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 0 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if ((DecimalPlaces > 0
                        && _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                                StringComparison.Ordinal))
                                || _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator,
                                StringComparison.Ordinal))
                    {
                        _textBox.CaretIndex++;
                        e.Handled = true;
                        break;
                    }
                    break;
                case Key.Back:
                    _Position.Key = CurrentKey.Back;
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 1 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if (DecimalPlaces > 0
                        && _textBox.CaretIndex ==
                        _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                            StringComparison.Ordinal) + 1)
                    {
                        _textBox.CaretIndex--;
                        e.Handled = true;
                        break;
                    }
                    setPositionOffset();
                    break;
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) => _textBox.SelectAll();
        #endregion

        #region Private procedures
        private void setPositionOffset()
        {
            if ((_textBox.Text == "-" && _Position.Key != CurrentKey.Decimal) || _textBox.Text.Length == _textBox.SelectionLength || Value == null)
            {
                _Position.Exclude = true;
            }

            if (_textBox.Text == "-" && _Position.Key == CurrentKey.Decimal)
            {
                if (DecimalPlaces > 0)
                {
                    _Position.Offset = (int)DecimalPlaces;
                    return;
                }
            }

            var sepPos = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            _Position.Offset = _textBox.Text.Length == _textBox.SelectionLength
                ? _textBox.Text.Length - 1
                : sepPos == -1
                    ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                    : _textBox.CaretIndex <= sepPos
                        ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                        : _Position.Key == CurrentKey.Number
                            ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) - 1
                            : _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) + 1;
        }

        private void cutCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _Position.Offset = 0;
            _Position.Exclude = false;
            _Position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            Clipboard.SetText(_textBox.SelectedText);
            if (_textBox.SelectionLength != _textBox.Text.Length)
                _textBox.Text = _textBox.Text.Substring(0, _textBox.SelectionStart) + _textBox.Text.Substring(_textBox.SelectionStart + _textBox.SelectionLength);
            else
                Value = null;
        }

        private void pasteCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _Position.Offset = 0;
            _Position.Exclude = false;
            _Position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!decimal.TryParse(text, out _))
                {
                    e.Handled = true;
                }
                else
                {
                    _Position.Key = CurrentKey.Number;
                    setPositionOffset();
                    if (_textBox.SelectionLength > 0)
                        _textBox.SelectedText = text;
                    else
                        _textBox.Text = _textBox.Text.Insert(_textBox.CaretIndex, text);
                }
            }
            else
            {
                e.Handled = true;
            }
        }
        #endregion
    }

    internal static class Extensions
    {
        internal static bool In<T>(this T obj, params T[] values) => values.Contains(obj);
    }

    /// <summary>
    /// 
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
            if (values[0] is not decimal decimalValue || values[1] is not Brush foregroundBrush || values[2] is not Brush negativeBrush) return null;
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

    /// <summary>
    /// Converts NumericBox value/text to string/decimal
    /// </summary>
    public class NumericBoxTextToValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts decimal value to string.
        /// </summary>
        /// <param name="values">Array consists of current NumericBox value, decimal places and separator using flag.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Formatted string.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not decimal decimalValue || values[1] is not uint decimalPlaces || values[2] is not bool useSeparator) return "";
            if (decimalValue == Statics.Epsilon)
                return "-";
            var partInt = decimal.Truncate(decimalValue);
            var partFraction =
                Math.Abs(decimal.Truncate((decimalValue - partInt) * (int)Math.Pow(10.0, decimalPlaces)));
            var formatInt = useSeparator ? "#" + culture.NumberFormat.NumberGroupSeparator + "##0" : "##0";
            var formatFraction = new string('0', (int)decimalPlaces);
            var stringInt = partInt.ToString(formatInt);
            if (decimalValue < 0 && partInt == 0)
                stringInt = $"-{stringInt}";
            var result = decimalPlaces > 0
                ? $"{stringInt}{culture.NumberFormat.NumberDecimalSeparator}{partFraction.ToString(formatFraction)}"
                : stringInt;
            return result;
        }

        /// <summary>
        /// Converts string to decimal.
        /// </summary>
        /// <param name="value">String.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Decimal.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is not string stringValue) return null;
            if (!string.IsNullOrEmpty(stringValue))
                stringValue = stringValue.Replace(culture.NumberFormat.NumberGroupSeparator, "");
            else
                return null;
            object[] result;
            if (stringValue != "-")
            {
                if (stringValue == "-.")
                {
                    result = new object[] { -Statics.Epsilon };
                }
                else if (double.TryParse(stringValue, out double doubleValue))
                {
                    if (doubleValue <= (double)decimal.MaxValue && doubleValue >= (double)decimal.MinValue)
                        result = new object[] { decimal.Parse(stringValue, NumberStyles.Any) };
                    else if (doubleValue > (double)decimal.MaxValue)
                        result = new object[] { decimal.MaxValue };
                    else
                        result = new object[] { decimal.MinValue };
                }
                else
                {
                    result = null;
                }
            }
            else
            {
                result = new object[] { Statics.Epsilon };
            }
            return result;
        }
#nullable restore
    }
}
