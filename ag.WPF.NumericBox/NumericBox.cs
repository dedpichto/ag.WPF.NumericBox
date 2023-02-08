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
        #region Constants
        private const string ElementText = "PART_Text";
        #endregion

        #region Elements
        private TextBox _textBox;
        #endregion

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(NumericBox),
                new FrameworkPropertyMetadata(0m, OnValueChanged));
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
        #endregion

        #region Public properties
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
        #endregion

        #region Callbacks
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
        #endregion

        #region Routed events
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
                setTextBinding();
            }
        }
        #endregion

        #region Event handlers
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox.Text == "-")
            {
                Value = null;
                setTextBinding();
                BindingOperations.GetBindingExpression(_textBox, TextBox.TextProperty).UpdateSource();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var bd = BindingOperations.GetBinding(_textBox, TextBox.TextProperty);
            if (bd != null && bd.Converter == null && _textBox.Text != "-")
            {
                var caretPos = _textBox.CaretIndex;
                setTextBinding();
                _textBox.CaretIndex = caretPos;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator)
            {
                e.Handled = true;
                return;
            }
            else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
            {
                if (DecimalPlaces > 0)
                {
                    _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                }
                e.Handled = true;
                return;
            }
            else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
            {
                if (_textBox.CaretIndex > 0)
                {
                    e.Handled = true;
                    return;
                }
                if (_textBox.SelectionLength == _textBox.Text.Length)
                    Value = null;
                if (Value == null)
                {
                    setTextBinding(true);
                    return;
                }
            }
            else if (!e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
            {
                e.Handled = true;
                return;
            }
            else if (e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9")
                && DecimalPlaces > 0
                && _textBox.CaretIndex == _textBox.Text.Length)
            {
                e.Handled = true;
                return;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (e.Key != Key.Home && e.Key != Key.End)
                    e.Handled = true;
                return;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.A, Key.C, Key.V))
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
                    if (DecimalPlaces > 0
                        && _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                                StringComparison.Ordinal))
                    {
                        _textBox.CaretIndex++;
                        e.Handled = true;
                        break;
                    }
                    break;
                case Key.Back:
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
                    break;
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) => _textBox.SelectAll();
        #endregion

        #region Private procedures
        private void pasteCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!decimal.TryParse(text, out decimal value))
                    e.Handled = true;
                else
                    Value = value;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void setTextBinding(bool removeConverter = false)
        {
            if (_textBox == null) return;

            var binding = new Binding(nameof(Value)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, RelativeSource = new RelativeSource { Mode = RelativeSourceMode.TemplatedParent } };
            if (removeConverter)
            {
                _textBox.SetBinding(TextBox.TextProperty, binding);
                return;
            }
            binding.Converter = new NumericBoxTextToValueConverter();
            if (UseGroupSeparator && DecimalPlaces > 0)
                binding.StringFormat = $"#{CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator}###{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}{new string('0', (int)DecimalPlaces)}";
            else if (!UseGroupSeparator && DecimalPlaces > 0)
                binding.StringFormat = $"#{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}{new string('0', (int)DecimalPlaces)}";
            else if (UseGroupSeparator && DecimalPlaces == 0)
                binding.StringFormat = $"#{CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator}###";
            else
                binding.StringFormat = "#";
            _textBox.SetBinding(TextBox.TextProperty, binding);
        }
        #endregion
    }

    internal static class Extensions
    {
        internal static bool In<T>(this T obj, params T[] values) => values.Contains(obj);
    }

    /// <summary>
    /// Converts NumericBox value/text to string/decimal
    /// </summary>
    public class NumericBoxTextToValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts NumericBox value to string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not decimal decimalValue) return null;
            return value == null ? "" : decimalValue;
        }
        /// <summary>
        /// Converts NumericBox text to decimal
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string stringValue) return null;
            stringValue.Replace(",,", ",");
            while (stringValue.StartsWith(","))
                stringValue = stringValue.Substring(1);
            return string.IsNullOrEmpty(stringValue) ? null : decimal.Parse(stringValue, NumberStyles.Any);
        }
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
#nullable restore
    }
}
