using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
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

        private struct CurrentPosition
        {
            public CurrentKey Key;
            public int Offset;
            public bool Exclude;
        }

        #region Constants
        private const string _elementText = "PART_Text";
        #endregion

        #region Elements
        private TextBox _textBox;
        #endregion

        private CurrentPosition _position;
        private bool _gotFocus;
        private bool _userInput;

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
                if (value != null && value.StartsWith(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator))
                {
                    value = value.Substring(1);
                }
                if (!string.IsNullOrEmpty(value) && !decimal.TryParse(value, out _) && !value.In(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}"))
                {
                    throw new FormatException("Input string was not in a correct format.");
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
            box.Text = box.convertToString((decimal?)e.NewValue);
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
            box.OnShowTrailingZerosChanged((bool)e.OldValue, (bool)e.NewValue);
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
            if (box._textBox != null)
                box._textBox.Text = (string)e.NewValue;
            box.Value = box.convertToDecimal((string)e.NewValue);
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
                _textBox.PreviewTextInput -= textBox_PreviewTextInput;
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
                _textBox.PreviewTextInput += textBox_PreviewTextInput;
                _textBox.TextChanged += textBox_TextChanged;
                _textBox.PreviewMouseLeftButtonDown += textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, pasteCommandBinding));
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, cutCommandBinding));
                BindingOperations.SetBinding(this, TextProperty, new Binding("Text") { Source = _textBox, Mode = BindingMode.OneWay });
                Value = v;
            }
        }
        #endregion

        #region Event handlers
        private void textBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                _textBox.SelectAll();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = false;
            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
            {
                Value = null;
            }
            else if (_textBox.Text.EndsWith(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) && !ShowTrailingZeros)
            {
                _textBox.Text = _textBox.Text.Substring(0, _textBox.Text.Length - 1);
            }
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
                if (_position.Exclude)
                    return;
                if (_position.Key.In(CurrentKey.Number, CurrentKey.Back, CurrentKey.Decimal))
                {
                    if (_textBox.Text.Length >= _position.Offset)
                    {
                        _textBox.CaretIndex = _textBox.Text.Length - _position.Offset;
                    }
                }
                return;
            }

            if (_position.Exclude)
                return;
            if (_position.Key.In(CurrentKey.Number, CurrentKey.Back, CurrentKey.Decimal))
            {
                if (_textBox.Text.Length >= _position.Offset)
                {
                    _textBox.CaretIndex = _textBox.Text.Length - _position.Offset;
                }
            }
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                var sepPos = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                _gotFocus = false;
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

                    //if (ShowTrailingZeros)
                    //{
                    //    if (_textBox.Text != CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                    //    {
                    //        _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                    //        e.Handled = true;
                    //    }
                    //    else
                    //    {
                    //        _userInput = true;
                    //        _position.Key = CurrentKey.Decimal;
                    //    }
                    //}
                    //else
                    //{
                    //    if (_textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) == -1)
                    //    {
                    //        _userInput = true;
                    //        _position.Key = CurrentKey.Decimal;
                    //    }
                    //    else
                    //    {
                    //        _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                    //        e.Handled = true;
                    //    }
                    //}
                    //return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                {
                    if (_textBox.SelectionLength == _textBox.Text.Length)
                    {
                        _userInput = true;
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

                //if (ShowTrailingZeros && e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9")
                //    && _textBox.Text.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                //    && _textBox.CaretIndex == _textBox.Text.Length)
                //{
                //    e.Handled = true;
                //    return;
                //}
                //else
                //{
                //    _userInput = true;
                //    _position.Key = CurrentKey.Number;
                //    //_position.Offset = 1;
                //    step = 1;
                //}
                if (e.Text.In("1", "2", "3", "4", "5", "6", "7", "8", "9"))
                {
                    _userInput = true;
                    _position.Key = CurrentKey.Number;
                }
                else if (e.Text == "0")
                {
                    _userInput = true;
                    _position.Key = CurrentKey.Number;
                    if (DecimalPlaces > 0)
                    {
                        var parts = _textBox.Text.Split(new char[] { CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0] });
                        if (parts.Length == 2 && parts[1].Length == DecimalPlaces)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
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

        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _gotFocus = false;
            _position.Key = CurrentKey.None;
            _position.Offset = 0;
            _position.Exclude = false;

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
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var thousandsSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
            var isNegative = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) >= 0;
            var decimalIndex = _textBox.Text.IndexOf(decimalSeparator, StringComparison.Ordinal);
            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    break;
                case Key.Delete:
                    _position.Key = CurrentKey.Delete;
                    _userInput = true;
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 0 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if (_textBox.Text[1] == thousandsSeparator[0])
                    {
                        _textBox.Text = _textBox.Text.Substring(2);
                        e.Handled = true;
                        break;
                    }
                    if (DecimalPlaces > 0)
                    {
                        if (_textBox.CaretIndex == decimalIndex || _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, StringComparison.Ordinal))
                        {
                            {
                                _textBox.CaretIndex++;
                                e.Handled = true;
                                break;
                            }
                        }
                        else if (_textBox.Text.EndsWith(decimalSeparator))
                        {
                            if (!isNegative && _textBox.CaretIndex == decimalIndex - 1 && _textBox.CaretIndex == 0)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && _textBox.CaretIndex == decimalIndex - 1 && _textBox.CaretIndex == 1)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (!isNegative && _textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && ((_textBox.SelectionStart == 1 && _textBox.SelectionLength == decimalIndex - 1) || (_textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)))
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                    break;
                case Key.Back:
                    _position.Key = CurrentKey.Back;
                    _userInput = true;
                    if (_textBox.CaretIndex > 0)
                    {
                        if (_textBox.Text.Length > 0 && _textBox.Text[_textBox.CaretIndex - 1] == thousandsSeparator[0])
                        {
                            _textBox.CaretIndex--;
                            setPositionOffset();
                            break;
                        }
                        if (_textBox.Text.Length > 0 && _textBox.Text[_textBox.CaretIndex - 1] == decimalSeparator[0])
                        {
                            if (isNegative)
                            {
                                if (_textBox.CaretIndex == 3 && _textBox.Text.Length == 3)
                                {
                                    Value = null;
                                    e.Handled = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (_textBox.CaretIndex == 2 && _textBox.Text.Length == 2)
                                {
                                    Value = null;
                                    e.Handled = true;
                                    break;
                                }
                            }
                            _textBox.CaretIndex--;
                            setPositionOffset();
                            break;
                        }
                        else if (_textBox.Text.Length > _textBox.CaretIndex && _textBox.Text[_textBox.CaretIndex] == decimalSeparator[0])
                        {
                            if (isNegative)
                            {
                                if (_textBox.CaretIndex == 2 && _textBox.Text.Length == 3)
                                {
                                    Value = null;
                                    e.Handled = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (_textBox.CaretIndex == 1 && _textBox.Text.Length == 2)
                                {
                                    Value = null;
                                    e.Handled = true;
                                    break;
                                }
                            }
                            //_textBox.CaretIndex--;
                            //setPositionOffset();
                            //break;
                        }
                    }
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 1 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    //if (DecimalPlaces > 0)
                    //{
                    //    var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    //    var isNegative = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) >= 0;
                    //    var decimalIndex = _textBox.Text.IndexOf(decimalSeparator, StringComparison.Ordinal);
                    //    if (_textBox.CaretIndex == decimalIndex + 1)
                    //    {
                    //        //if (_textBox.CaretIndex > 0)
                    //        //{
                    //        //    _textBox.CaretIndex--;
                    //        //}
                    //        //e.Handled = true;
                    //        _textBox.CaretIndex--;
                    //        break;
                    //    }
                    //    else if (_textBox.Text.EndsWith(decimalSeparator))
                    //    {
                    //        if (!isNegative && _textBox.CaretIndex == decimalIndex && _textBox.CaretIndex == 1)
                    //        {
                    //            _userInput = true;
                    //            Value = null;
                    //            e.Handled = true;
                    //            break;
                    //        }
                    //        else if (isNegative && _textBox.CaretIndex == decimalIndex && _textBox.CaretIndex == 2)
                    //        {
                    //            _userInput = true;
                    //            Value = null;
                    //            e.Handled = true;
                    //            break;
                    //        }
                    //        else if (!isNegative && _textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)
                    //        {
                    //            _userInput = true;
                    //            Value = null;
                    //            e.Handled = true;
                    //            break;
                    //        }
                    //        else if (isNegative && ((_textBox.SelectionStart == 1 && _textBox.SelectionLength == decimalIndex - 1) || (_textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)))
                    //        {
                    //            _userInput = true;
                    //            Value = null;
                    //            e.Handled = true;
                    //            break;
                    //        }
                    //    }
                    //}
                    setPositionOffset();
                    break;
                case Key.OemPeriod or Key.Decimal:
                    if (_textBox.Text.IndexOf(decimalSeparator) >= 0)
                    {
                        e.Handled = true;
                    }
                    else if ((string.IsNullOrEmpty(_textBox.Text) || _textBox.SelectionLength == _textBox.Text.Length) && DecimalPlaces > 0)
                    {
                        e.Handled = true;
                    }
                    else
                    {
                        _userInput = true;
                        _position.Key = CurrentKey.Decimal;
                    }
                    break;
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = true;
            _textBox.SelectAll();
        }
        #endregion

        #region Private procedures
        private void setPositionOffset()
        {
            //if (!ShowTrailingZeros) return;
            if ((_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key != CurrentKey.Decimal) || (_textBox.Text.Length == _textBox.SelectionLength) || (Value == null))
            {
                _position.Exclude = true;
            }

            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key == CurrentKey.Decimal)
            {
                if (DecimalPlaces > 0)
                {
                    _position.Offset = (int)DecimalPlaces;
                    return;
                }
            }

            var sepPos = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            if (_textBox.Text.Length == _textBox.SelectionLength)
            {
                _position.Offset = _textBox.Text.Length - 1;
            }
            else if (sepPos == -1)
            {
                _position.Offset = _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength);
                //if (step != 0)
                //    _position.Offset = step;
                //else
                //    _position.Offset = _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength);
            }
            else
            {
                if (_textBox.CaretIndex <= sepPos)
                {
                    //if (step != 0)
                    //    _position.Offset = step;
                    //else
                    _position.Offset = _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength);
                }
                else
                {
                    if (_position.Key == CurrentKey.Number || _position.Key == CurrentKey.Back)
                    {
                        //if (step != 0)
                        //    _position.Offset = step - 1;
                        //else
                        _position.Offset = _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) - 1;
                    }
                    else
                    {
                        //if (step != 0)
                        //    _position.Offset = step - 1;
                        //else
                        _position.Offset = _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) + 1;
                    }
                }
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
            if (arr.Length == 2)
                return (arr[0], arr[1]);
            return (arr[0], "");
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

        private enum SpecialCases
        {
            None,
            Negative,
            NegativeZero,
            NegativeDot,
            Dot,
            EndDot
        }
        private SpecialCases _specialCases;

        private string convertToString(decimal? decimalValue)
        {
            var culture = CultureInfo.CurrentCulture;
            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;

            if (decimalValue == null)
            {
                if (_specialCases == SpecialCases.Negative && _userInput)
                {
                    return culture.NumberFormat.NegativeSign;
                }
                else if (_specialCases == SpecialCases.NegativeZero && _userInput)
                {
                    return $"{culture.NumberFormat.NegativeSign}0";
                }
                else if (_specialCases == SpecialCases.NegativeDot && _userInput)
                {
                    return $"{culture.NumberFormat.NegativeSign}{decimalSeparator}";
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
            var isFocused = _textBox != null && _textBox.IsFocused;

            var formatInt = UseGroupSeparator ? "#" + culture.NumberFormat.NumberGroupSeparator + "##0" : "##0";
            var formatFraction = new string('0', (int)DecimalPlaces);

            var (intpPart, fracPart) = getDecimalParts(decimalValue.Value, culture);
            if (_specialCases == SpecialCases.EndDot && _userInput)
            {
                intpPart = Convert.ToInt64(intpPart).ToString(formatInt);
                return $"{intpPart}{decimalSeparator}";
            }
            //var partInt = decimal.Truncate(decimalValue.Value);
            ////var fractionCount = TruncateFractionalPart ? DecimalPlaces : BitConverter.GetBytes(decimal.GetBits(decimalValue.Value)[3])[2];
            //var fractionCount = BitConverter.GetBytes(decimal.GetBits(decimalValue.Value)[3])[2];

            //var partFraction = Math.Abs(decimal.Truncate((decimalValue.Value - partInt) * (int)Math.Pow(10.0, fractionCount)));

            //var formatFractionFull = new string('0', (int)DecimalPlaces);
            //var stringInt = partInt.ToString(formatInt);
            //var realDecimalString = getRealFractionString(decimalValue.Value,culture);

            string result;
            if (TruncateFractionalPart)
            {
                if (DecimalPlaces > 0)
                {
                    if (fracPart.Length > DecimalPlaces)
                        fracPart = fracPart.Substring(0, (int)DecimalPlaces);
                    intpPart = Convert.ToInt64(intpPart).ToString(formatInt);
                    result = $"{intpPart}{decimalSeparator}{fracPart}";
                }
                else
                {
                    result = Convert.ToInt64(intpPart).ToString(formatInt);
                }
                //var stringFraction = partFraction.ToString(formatFraction);
                //if (realDecimalString == "0")
                //{
                //    stringFraction = "0";
                //}
                //else
                //{
                //    var stringFractionFull = partFraction.ToString(formatFractionFull);
                //    var diff = stringFractionFull.Length - stringFraction.Length;
                //    if (diff > 0 && partFraction != 0)
                //    {
                //        stringFraction = $"{new string('0', diff)}{stringFraction}";
                //    }
                //    if (stringFraction.Length > DecimalPlaces)
                //    {
                //        stringFraction = stringFraction.Substring(0, (int)DecimalPlaces);
                //    }
                //}

                //if (!ShowTrailingZeros && DecimalPlaces > 0 && stringFraction.EndsWith("0"))
                //{

                //    if (realDecimalString == null || realDecimalString.Length >= DecimalPlaces)
                //    {
                //        while (!stringFraction.EndsWith($"{decimalSeparator}0"))
                //        {
                //            stringFraction = stringFraction.Substring(0, stringFraction.Length - 1);
                //            if (!stringFraction.EndsWith("0"))
                //                break;
                //        }
                //    }
                //    else
                //    {
                //        stringFraction = realDecimalString;
                //    }
                //}
                //if (decimalValue < 0 && partInt == 0)
                //    stringInt = $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{stringInt}";
                //result = DecimalPlaces > 0
                //    ? string.IsNullOrEmpty(stringFraction) && !isFocused ? stringInt : $"{stringInt}{decimalSeparator}{stringFraction}"
                //    : stringInt;
            }
            else
            {
                //var wholeFraction = partFraction / (decimal)Math.Pow(10, fractionCount);
                //var wholeNumber = partInt >= 0 ? partInt + wholeFraction : partInt - wholeFraction;
                var format = $"{formatInt}{decimalSeparator}{formatFraction}";

                result = decimalValue.Value.ToString(format);
                //if (!ShowTrailingZeros && DecimalPlaces > 0 && result.EndsWith("0"))
                //{
                //    while (!result.EndsWith($"{decimalSeparator}0"))
                //    {
                //        result = result.Substring(0, result.Length - 1);
                //        if (!result.EndsWith("0"))
                //            break;
                //    }
                //}
                //if (decimalValue < 0 && partInt == 0)
                //    result = $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{result}";
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
            _userInput = false;
            return result;
        }

        private decimal? convertToDecimal(string stringValue)
        {
            _specialCases = SpecialCases.None;
            var culture = CultureInfo.CurrentCulture;
            if (!string.IsNullOrEmpty(stringValue))
                stringValue = stringValue.Replace(culture.NumberFormat.NumberGroupSeparator, "");
            else
                return null;

            decimal? result = null;

            if (stringValue == culture.NumberFormat.NegativeSign)
            {
                _specialCases = SpecialCases.Negative;
                return null;
            }
            else if (stringValue == $"{culture.NumberFormat.NegativeSign}{culture.NumberFormat.NumberDecimalSeparator}")
            {
                _specialCases = SpecialCases.NegativeDot;
                return null;
            }
            else if (stringValue == culture.NumberFormat.NumberDecimalSeparator)
            {
                _specialCases = SpecialCases.Dot;
                return null;
            }
            else if (stringValue == $"{culture.NumberFormat.NegativeSign}0")
            {
                _specialCases = SpecialCases.NegativeZero;
                return null;
            }
            else if (stringValue.EndsWith(culture.NumberFormat.NumberDecimalSeparator))
            {
                _specialCases = SpecialCases.EndDot;
                result = getDecimalFromString(stringValue.Substring(0, stringValue.Length - 1));
            }
            else if (stringValue.StartsWith($"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}"))
            {
                if (stringValue == $"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}")
                {
                    result = 0;
                }
                else
                {
                    var arr = stringValue.Split(culture.NumberFormat.NumberDecimalSeparator[0]);
                    if (arr.Length == 2 && arr[1].All(c => c == '0'))
                    {
                        result = 0;
                    }
                    else
                    {
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
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

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
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

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
                    _position.Key = CurrentKey.Number;
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
}
