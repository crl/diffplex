using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace DiffPlex.Wpf.Controls
{
    /// <summary>
    /// The diff control for text.
    /// </summary>
    public partial class DiffViewer : UserControl
    {
        /// <summary>
        /// The event arguments of view mode changed.
        /// </summary>
        public class ViewModeChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the ViewModeChangedEventArgs class.
            /// </summary>
            /// <param name="isSideBySide">true if it is side-by-side view mode.</param>
            public ViewModeChangedEventArgs(bool isSideBySide)
            {
                IsSideBySideViewMode = isSideBySide;
                IsInlineViewMode = !isSideBySide;
            }

            /// <summary>
            /// Gets a value indicating whether it is side-by-side (split) view mode.
            /// </summary>
            public bool IsSideBySideViewMode { get; }

            /// <summary>
            /// Gets a value indicating whether it is inline (unified) view mode.
            /// </summary>
            public bool IsInlineViewMode { get; }
        }

        /// <summary>
        /// The property of old text.
        /// </summary>
        public static readonly DependencyProperty OldTextProperty = RegisterRefreshDependencyProperty<string>(nameof(OldText), null);

        /// <summary>
        /// The property of new text.
        /// </summary>
        public static readonly DependencyProperty NewTextProperty = RegisterRefreshDependencyProperty<string>(nameof(NewText), null);

        /// <summary>
        /// The property of a flag to ignore white space.
        /// </summary>
        public static readonly DependencyProperty IgnoreWhiteSpaceProperty = RegisterRefreshDependencyProperty(nameof(IgnoreWhiteSpace), true);

        /// <summary>
        /// The property of a flag to ignore case.
        /// </summary>
        public static readonly DependencyProperty IgnoreCaseProperty = RegisterRefreshDependencyProperty(nameof(IgnoreCase), false);

        /// <summary>
        /// The property of line number background brush.
        /// </summary>
        public static readonly DependencyProperty LineNumberForegroundProperty = RegisterDependencyProperty<Brush>(nameof(LineNumberForeground), new SolidColorBrush(Color.FromArgb(255, 64, 128, 160)));

        /// <summary>
        /// The property of line number width.
        /// </summary>
        public static readonly DependencyProperty LineNumberWidthProperty = RegisterDependencyProperty(nameof(LineNumberWidth), 60, (d, e) =>
        {
            if (!(d is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is int n)) return;
            c.LeftContentPanel.LineNumberWidth = c.RightContentPanel.LineNumberWidth = c.InlineContentPanel.LineNumberWidth = n;
        });

        /// <summary>
        /// The property of change type symbol foreground brush.
        /// </summary>
        public static readonly DependencyProperty ChangeTypeForegroundProperty = RegisterDependencyProperty<Brush>(nameof(ChangeTypeForeground), new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));

        /// <summary>
        /// The property of old text header.
        /// </summary>
        public static readonly DependencyProperty OldTextHeaderProperty = RegisterDependencyProperty<string>(nameof(OldTextHeader), null, (d, e) =>
        {
            if (!(d is DiffViewer c) || e.OldValue == e.NewValue) return;
            c.UpdateHeaderText();
        });

        /// <summary>
        /// The property of new text header.
        /// </summary>
        public static readonly DependencyProperty NewTextHeaderProperty = RegisterDependencyProperty<string>(nameof(NewTextHeader), null, (d, e) =>
        {
            if (!(d is DiffViewer c) || e.OldValue == e.NewValue) return;
            c.UpdateHeaderText();
        });

        /// <summary>
        /// The property of header height.
        /// </summary>
        public static readonly DependencyProperty HeaderHeightProperty = RegisterDependencyProperty<double>(nameof(HeaderHeight), 0, (d, e) =>
        {
            if (!(d is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is double n)) return;
            c.HeaderRow.Height = new GridLength(n);
            c.isHeaderEnabled = true;
        });

        /// <summary>
        /// The property of header background brush.
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty = RegisterDependencyProperty<Brush>(nameof(HeaderForeground), new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));

        /// <summary>
        /// The property of header background brush.
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(HeaderBackground), new SolidColorBrush(Color.FromArgb(12, 128, 128, 128)));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty InsertedForegroundProperty = RegisterDependencyProperty<Brush>(nameof(InsertedForeground));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty InsertedBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(InsertedBackground), new SolidColorBrush(Color.FromArgb(64, 96, 216, 32)));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty DeletedForegroundProperty = RegisterDependencyProperty<Brush>(nameof(DeletedForeground));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty DeletedBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(DeletedBackground), new SolidColorBrush(Color.FromArgb(64, 216, 32, 32)));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty UnchangedForegroundProperty = RegisterDependencyProperty<Brush>(nameof(UnchangedForeground));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty UnchangedBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(UnchangedBackground));

        /// <summary>
        /// The property of text inserted background brush.
        /// </summary>
        public static readonly DependencyProperty ImaginaryBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(ImaginaryBackground), new SolidColorBrush(Color.FromArgb(24, 128, 128, 128)));

        /// <summary>
        /// The property of grid splitter background brush.
        /// </summary>
        public static readonly DependencyProperty SplitterBackgroundProperty = RegisterDependencyProperty<Brush>(nameof(SplitterBackground), new SolidColorBrush(Color.FromArgb(64, 128, 128, 128)));

        /// <summary>
        /// The property of grid splitter border brush.
        /// </summary>
        public static readonly DependencyProperty SplitterBorderBrushProperty = RegisterDependencyProperty<Brush>(nameof(SplitterBorderBrush));

        /// <summary>
        /// The property of grid splitter border thickness.
        /// </summary>
        public static readonly DependencyProperty SplitterBorderThicknessProperty = RegisterDependencyProperty<Thickness>(nameof(SplitterBorderThickness));

        /// <summary>
        /// The property of grid splitter width.
        /// </summary>
        public static readonly DependencyProperty SplitterWidthProperty = RegisterDependencyProperty<double>(nameof(SplitterWidth), 5);

        /// <summary>
        /// The property of flag of hiding unchanged lines
        /// </summary>
        public static readonly DependencyProperty IgnoreUnchangedProperty = RegisterDependencyProperty(nameof(IgnoreUnchanged), false, (o, e) =>
        {
            if (!(o is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is bool b))
                return;
            if (b)
            {
                var lines = c.LinesContext;
                Helper.CollapseUnchangedSections(c.LeftContentPanel, lines);
                Helper.CollapseUnchangedSections(c.RightContentPanel, lines);
                Helper.CollapseUnchangedSections(c.InlineContentPanel, lines);
                c.CollapseUnchangedSectionsToggle.IsChecked = true;
                c.ContextLinesMenuItems.Visibility = Visibility.Visible;
            }
            else
            {
                Helper.ExpandUnchangedSections(c.LeftContentPanel);
                Helper.ExpandUnchangedSections(c.RightContentPanel);
                Helper.ExpandUnchangedSections(c.InlineContentPanel);
                c.CollapseUnchangedSectionsToggle.IsChecked = false;
                c.ContextLinesMenuItems.Visibility = Visibility.Collapsed;
            }
        });

        /// <summary>
        /// The property of flag of lines count that will be displayed before and after of unchanged line
        /// </summary>
        public static readonly DependencyProperty LinesContextProperty = RegisterDependencyProperty(nameof(LinesContext), 1, (o, e) =>
        {
            if (!(o is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is int i))
                return;
            if (i < 0) i = 0;
            if (c.IgnoreUnchanged)
            {
                Helper.CollapseUnchangedSections(c.LeftContentPanel, i);
                Helper.CollapseUnchangedSections(c.RightContentPanel, i);
                Helper.CollapseUnchangedSections(c.InlineContentPanel, i);
            }

            c.RefreshContextLinesMenuItemState(i);
        });

        public static readonly DependencyProperty HistoryPositionProperty = RegisterDependencyProperty(nameof(HistoryPosition),0,
            (o, e) =>
            {
                if (!(o is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is int newValue))
                {
                    return;
                }

                c.HistoryMessage = (newValue+1) + "/" + c.undoStack.Count;
            });

        public static readonly DependencyProperty HistoryMessageProperty = RegisterDependencyProperty(nameof(HistoryMessage), "0/0", (o, e) =>
            { });

        public static readonly DependencyProperty FilterNameProperty = RegisterDependencyProperty(nameof(FilterName),
            "100009184", (o, e) =>
            {
                if (!(o is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is string newValue))
                {
                    return;
                }

                try
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(newValue);
                    var value = xml.FirstChild.Attributes["Name"].InnerText;

                    if (value != null)
                    {
                        newValue = value;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }

                c.DoByChange(newValue);
            });
        /// <summary>
        /// The property of IsSideBySide.
        /// </summary>
        public static readonly DependencyProperty IsSideBySideProperty = RegisterDependencyProperty(nameof(IsSideBySide), true, (d, e) =>
        {
            if (!(d is DiffViewer c) || e.OldValue == e.NewValue || !(e.NewValue is bool b)) return;
            c.SideBySideModeToggle.IsChecked = b;
            c.InlineModeToggle.IsChecked = !b;
            c.ChangeViewMode(b);
        });

        /// <summary>
        /// The side-by-side diffs result.
        /// </summary>
        private SideBySideDiffModel sideBySideResult;

        /// <summary>
        /// The inline diffs result.
        /// </summary>
        private DiffPaneModel inlineResult;

        /// <summary>
        /// The flag to enable header.
        /// </summary>
        private bool isHeaderEnabled;

        private XmlNode OldXmlNode;
        private XmlNode NewXmlNode;

        /// <summary>
        /// Initializes a new instance of the DiffViewer class.
        /// </summary>
        public DiffViewer()
        {
            InitializeComponent();

            LeftContentPanel.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            LeftContentPanel.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this, Mode = BindingMode.OneWay });
            RightContentPanel.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this, Mode = BindingMode.OneWay });
            InlineContentPanel.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this, Mode = BindingMode.OneWay });
            Splitter.SetBinding(BackgroundProperty, new Binding(nameof(SplitterBackground)) { Source = this, Mode = BindingMode.OneWay });
            Splitter.SetBinding(BorderBrushProperty, new Binding(nameof(SplitterBorderBrush)) { Source = this, Mode = BindingMode.OneWay });
            Splitter.SetBinding(BorderThicknessProperty, new Binding(nameof(SplitterBorderThickness)) { Source = this, Mode = BindingMode.OneWay });
            Splitter.SetBinding(WidthProperty, new Binding(nameof(SplitterWidth)) { Source = this, Mode = BindingMode.OneWay });
            HeaderBorder.SetBinding(BackgroundProperty, new Binding(nameof(HeaderBackground)) { Source = this, Mode = BindingMode.OneWay });
            ApplyHeaderTextProperties(LeftHeaderText);
            ApplyHeaderTextProperties(RightHeaderText);
            ApplyHeaderTextProperties(InlineHeaderText);

            var contextMenu = Helper.CreateLineContextMenu(this);
            var searchMenuItem = new MenuItem
            {
                Header = Helper.GetButtonName("Filter this Name", "C")
            };
            contextMenu.Items.Add(searchMenuItem);

            var str = string.Empty;
            ContextMenuOpening += (sender, ev) =>
            {
                searchMenuItem.IsEnabled = false;
                var ele = ev.OriginalSource as FrameworkElement;
                while (ele != null && ele != this && !(ele is Window))
                {
                    if (ele.Tag is DiffPiece piece)
                    {
                        str = piece.Text;
                        break;
                    }

                    ele = ele.Parent as FrameworkElement;
                }

                searchMenuItem.IsEnabled = !string.IsNullOrWhiteSpace(str);
            };

            searchMenuItem.Click += (sender, ev) =>
            {
                if (string.IsNullOrEmpty(str)) return;

                var newValue = str;
                try
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(newValue);
                    var value = xml.FirstChild.Attributes["Name"].InnerText;

                    if (value != null)
                    {
                        newValue = value;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }

                this.FilterName = str;
            };

            InlineContentPanel.LineContextMenu =
                LeftContentPanel.LineContextMenu = RightContentPanel.LineContextMenu = contextMenu;
            InlineHeaderText.ContextMenu = LeftHeaderText.ContextMenu = RightHeaderText.ContextMenu = HeaderContextMenu;
            InlineModeToggle.Header = Helper.GetButtonName(Resource.InlineMode ?? "Unified view", "U");
            SideBySideModeToggle.Header = Helper.GetButtonName(Resource.SideBySideMode ?? "Split view", "S");
            CollapseUnchangedSectionsToggle.Header = Helper.GetButtonName(Resource.SkipUnchangedLines ?? "Collapse unchanged sections", "C");
            ContextLinesMenuItems.Header = Helper.GetButtonName(Resource.ContextLines ?? "Lines for context", "L");
            RefreshContextLinesMenuItemState(LinesContext);

            

            LeftContentPanel.OnFileChange = (file) =>
            {
                OldTextHeader = file;
                OldXmlNode = GetRootNode(file);
                if (OldXmlNode == null)
                {
                    OldText = GetFileText(file);
                    return;
                }

                var resultNode = GetResultNode(OldXmlNode);
                if (resultNode == null)
                {
                    OldText = GetFileText(file);
                    return;
                }

                OldText = PrintXML(resultNode.OuterXml);
            };
            RightContentPanel.OnFileChange = (file) =>
            {
                NewTextHeader = file;
                NewXmlNode = GetRootNode(file);
                if (NewXmlNode == null)
                {
                    NewText = GetFileText(file);
                    return;
                }
                var resultNode = GetResultNode(NewXmlNode);

                if (resultNode == null)
                {
                    NewText = GetFileText(file);
                    return;
                }

                NewText = PrintXML(resultNode.OuterXml);
            };
        }

        private const int MAX_UNDO = 100;
        public List<string> undoStack = new List<string>();
        public int HistoryPosition
        {
            get => (int)GetValue(HistoryPositionProperty);
            set
            {
                SetValue(HistoryPositionProperty, value);
            }
        }

        public string HistoryMessage
        {
            get => (string)GetValue(HistoryMessageProperty);
            set
            {
                SetValue(HistoryMessageProperty, value);
            }
        }
        private bool isRedo = false;

        public void Undo()
        {
            if (HistoryPosition > 0)
            {
                var name = undoStack[--HistoryPosition];
                Do(name);
            }
        }

        public void Redo()
        {
            if (HistoryPosition < undoStack.Count-1)
            {
                var name = undoStack[++HistoryPosition];
                Do(name);
            }
        }

        private void Do(string name)
        {
            isRedo = true;
            FilterName = name;
            isRedo = false;
            this.UpdateFilterName(name);
        }

        private void DoByChange(string newValue)
        {
            if (string.IsNullOrEmpty(newValue) == false && isRedo == false)
            {
                var len = undoStack.Count;
                for (int i = len - 1; i > HistoryPosition; i--)
                {
                    undoStack.RemoveAt(i);
                }
                undoStack.Add(newValue);

                if (undoStack.Count > MAX_UNDO)
                {
                    undoStack.RemoveAt(0);
                }

                HistoryPosition = undoStack.Count - 1;
            }

            UpdateFilterName(newValue);
        }

        private void UpdateFilterName(string newValue)
        {
            if (OldXmlNode != null)
            {
                var resultNode = GetResultNode(OldXmlNode,newValue);
                if (resultNode != null)
                {
                    var value = resultNode.OuterXml;
                    OldText = PrintXML(value);
                }
                else
                {
                    OldText = "";
                }
            }
            if (NewXmlNode != null)
            {
                var resultNode = GetResultNode(NewXmlNode,newValue);
                if (resultNode != null)
                {
                    var value = resultNode.OuterXml;
                    NewText = PrintXML(value);
                }
                else
                {
                    NewText = "";
                }
            }
        }

        private string GetFileText(string file)
        {
            if (File.Exists(file))
            {
                return File.ReadAllText(file,Encoding.UTF8);
            }

            return string.Empty;
        }

        private XmlNode GetRootNode(string file)
        {
            var xml = new XmlDocument();
            xml.Load(file);

            var xmlNode = xml.SelectSingleNode(singleKey);

            if (xmlNode == null)
            {
                MessageBox.Show(singleKey + "不存在");
                return null;
            }
            return xmlNode;
        }

        private XmlNode GetResultNode(XmlNode rootNode,string newValue="")
        {
            var list = rootNode.ChildNodes;

            if (string.IsNullOrEmpty(newValue))
            {
                newValue = FilterName;
            }

            XmlNode resultNode = list[0];
            foreach (XmlNode node in list)
            {
                if (node.Attributes["Name"].InnerText == newValue)
                {
                    resultNode = node;
                    break;
                }
            }
            return resultNode;
        }

        public static string PrintXML(string xml)
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException)
            {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();

            return result;
        }

        public string singleKey = "UnityGameFramework/BuildReport/AssetBundles";

        /// <summary>
        /// Occurs when the view mode is changed.
        /// </summary>
        public event EventHandler<ViewModeChangedEventArgs> ViewModeChanged;

        /// <summary>
        /// Occurs when the grid splitter loses mouse capture.
        /// </summary>
        [Category("Behavior")]
        public event DragCompletedEventHandler SplitterDragCompleted
        {
            add => Splitter.DragCompleted += value;
            remove => Splitter.DragCompleted -= value;
        }

        /// <summary>
        /// Occurs one or more times as the mouse changes position when the grid splitter has logical focus and mouse capture.
        /// </summary>
        [Category("Behavior")]
        public event DragDeltaEventHandler SplitterDragDelta
        {
            add => Splitter.DragDelta += value;
            remove => Splitter.DragDelta -= value;
        }

        /// <summary>
        /// Occurs when the grid splitter receives logical focus and mouse capture.
        /// </summary>
        [Category("Behavior")]
        public event DragStartedEventHandler SplitterDragStarted
        {
            add => Splitter.DragStarted += value;
            remove => Splitter.DragStarted -= value;
        }

        /// <summary>
        /// Gets or sets the old text.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public string OldText
        {
            get => (string)GetValue(OldTextProperty);
            set => SetValue(OldTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the new text.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public string NewText
        {
            get => (string)GetValue(NewTextProperty);
            set => SetValue(NewTextProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether ignore the white space.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public bool IgnoreWhiteSpace
        {
            get => (bool)GetValue(IgnoreWhiteSpaceProperty);
            set => SetValue(IgnoreWhiteSpaceProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether ignore case.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public bool IgnoreCase
        {
            get => (bool)GetValue(IgnoreCaseProperty);
            set => SetValue(IgnoreCaseProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line number.
        /// </summary>
        [Bindable(true)]
        public Brush LineNumberForeground
        {
            get => (Brush)GetValue(LineNumberForegroundProperty);
            set => SetValue(LineNumberForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the line number width.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public int LineNumberWidth
        {
            get => (int)GetValue(LineNumberWidthProperty);
            set => SetValue(LineNumberWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the change type symbol.
        /// </summary>
        [Bindable(true)]
        public Brush ChangeTypeForeground
        {
            get => (Brush)GetValue(ChangeTypeForegroundProperty);
            set => SetValue(ChangeTypeForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the header of the old text.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public string OldTextHeader
        {
            get => (string)GetValue(OldTextHeaderProperty);
            set => SetValue(OldTextHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the header of the new text.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public string NewTextHeader
        {
            get => (string)GetValue(NewTextHeaderProperty);
            set => SetValue(NewTextHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line added.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public double HeaderHeight
        {
            get => (double)GetValue(HeaderHeightProperty);
            set => SetValue(HeaderHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line added.
        /// </summary>
        [Bindable(true)]
        public Brush HeaderForeground
        {
            get => (Brush)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the line added.
        /// </summary>
        [Bindable(true)]
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line added.
        /// </summary>
        [Bindable(true)]
        public Brush InsertedForeground
        {
            get => (Brush)GetValue(InsertedForegroundProperty);
            set => SetValue(InsertedForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the line added.
        /// </summary>
        [Bindable(true)]
        public Brush InsertedBackground
        {
            get => (Brush)GetValue(InsertedBackgroundProperty);
            set => SetValue(InsertedBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line deleted.
        /// </summary>
        [Bindable(true)]
        public Brush DeletedForeground
        {
            get => (Brush)GetValue(DeletedForegroundProperty);
            set => SetValue(DeletedForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the line deleted.
        /// </summary>
        [Bindable(true)]
        public Brush DeletedBackground
        {
            get => (Brush)GetValue(DeletedBackgroundProperty);
            set => SetValue(DeletedBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the line unchanged.
        /// </summary>
        [Bindable(true)]
        public Brush UnchangedForeground
        {
            get => (Brush)GetValue(UnchangedForegroundProperty);
            set => SetValue(UnchangedForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the line unchanged.
        /// </summary>
        [Bindable(true)]
        public Brush UnchangedBackground
        {
            get => (Brush)GetValue(UnchangedBackgroundProperty);
            set => SetValue(UnchangedBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the line imaginary.
        /// </summary>
        [Bindable(true)]
        public Brush ImaginaryBackground
        {
            get => (Brush)GetValue(ImaginaryBackgroundProperty);
            set => SetValue(ImaginaryBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the grid splitter.
        /// </summary>
        [Bindable(true)]
        public Brush SplitterBackground
        {
            get => (Brush)GetValue(SplitterBackgroundProperty);
            set => SetValue(SplitterBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the grid splitter.
        /// </summary>
        [Bindable(true)]
        public Brush SplitterBorderBrush
        {
            get => (Brush)GetValue(SplitterBackgroundProperty);
            set => SetValue(SplitterBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the grid splitter.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public Thickness SplitterBorderThickness
        {
            get => (Thickness)GetValue(SplitterBorderThicknessProperty);
            set => SetValue(SplitterBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the grid splitter.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public double SplitterWidth
        {
            get => (double)GetValue(SplitterWidthProperty);
            set => SetValue(SplitterWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether it is in side-by-side (split) view mode to diff.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public bool IsSideBySide
        {
            get => (bool)GetValue(IsSideBySideProperty);
            set => SetValue(IsSideBySideProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether need collapse unchanged sections.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public bool IgnoreUnchanged
        {
            get => (bool)GetValue(IgnoreUnchangedProperty);
            set => SetValue(IgnoreUnchangedProperty, value);
        }

        /// <summary>
        /// Gets or sets the count of context line.
        /// The context line is the one unchanged arround others as their margin.
        /// </summary>
        [Bindable(true)]
        [Category("Appearance")]
        public int LinesContext
        {
            get => (int)GetValue(LinesContextProperty);
            set => SetValue(LinesContextProperty, value);
        }

        [Bindable(true)]
        [Category("Appearance")]
        public string FilterName
        {
            get => (string)GetValue(FilterNameProperty);
            set => SetValue(FilterNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name of inline mode toggle.
        /// </summary>
        [Category("Appearance")]
        public object InlineModeToggleTitle
        {
            get => InlineModeToggle.Header;
            set => InlineModeToggle.Header = value;
        }
        /// <summary>
        /// Gets or sets the display name of side by side mode toggle.
        /// </summary>
        [Category("Appearance")]
        public object SideBySideModeToggleTitle
        {
            get => SideBySideModeToggle.Header;
            set => SideBySideModeToggle.Header = value;
        }

        /// <summary>
        /// Gets or sets the display name of skip unchanged lines toggle.
        /// </summary>
        [Category("Appearance")]
        public object CollapseUnchangedSectionsToggleTitle
        {
            get => CollapseUnchangedSectionsToggle.Header;
            set => CollapseUnchangedSectionsToggle.Header = value;
        }

        /// <summary>
        /// Gets or sets the display name of context lines count.
        /// </summary>
        [Category("Appearance")]
        public object ContextLinesMenuItemsTitle
        {
            get => ContextLinesMenuItems.Header;
            set => ContextLinesMenuItems.Header = value;
        }

        /// <summary>
        /// Gets a value indicating whether the grid splitter has logical focus and mouse capture and the left mouse button is pressed.
        /// </summary>
        public bool IsSplitterDragging => Splitter.IsDragging;

        /// <summary>
        /// Gets a value that represents the actual calculated width of the left side panel.
        /// </summary>
        public double LeftSideActualWidth => LeftColumn.ActualWidth;

        /// <summary>
        /// Gets a value that represents the actual calculated width of the right side panel.
        /// </summary>
        public double RightSideActualWidth => RightColumn.ActualWidth;

        /// <summary>
        /// Gets a value indicating whether it is side-by-side (split) view mode.
        /// </summary>
        public bool IsSideBySideViewMode => InlineContentPanel.Visibility != Visibility.Visible;

        /// <summary>
        /// Gets a value indicating whether it is inline view mode.
        /// </summary>
        public bool IsInlineViewMode => InlineContentPanel.Visibility == Visibility.Visible;

        /// <summary>
        /// Gets the side-by-side diffs result.
        /// </summary>
        public SideBySideDiffModel GetSideBySideDiffModel()
        {
            if (sideBySideResult != null || OldText == null || NewText == null) return sideBySideResult;
            sideBySideResult = SideBySideDiffBuilder.Diff(OldText, NewText, IgnoreWhiteSpace, IgnoreCase);
            RenderSideBySideDiffs();
            return sideBySideResult;
        }

        /// <summary>
        /// Gets the inline diffs result.
        /// </summary>
        public DiffPaneModel GetInlineDiffModel()
        {
            if (inlineResult != null || OldText == null || NewText == null) return inlineResult;
            inlineResult = InlineDiffBuilder.Diff(OldText, NewText, IgnoreWhiteSpace, IgnoreCase);
            RenderInlineDiffs();
            return inlineResult;
        }

        /// <summary>
        /// Refreshes.
        /// </summary>
        public void Refresh()
        {
            if (InlineContentPanel.Visibility == Visibility.Visible)
            {
                sideBySideResult = null;
                RenderSideBySideDiffs();
                if (NewText == null || OldText == null)
                {
                    inlineResult = null;
                    RenderInlineDiffs();
                    return;
                }

                inlineResult = InlineDiffBuilder.Diff(OldText, NewText, IgnoreWhiteSpace, IgnoreCase);
                RenderInlineDiffs();
                return;
            }

            inlineResult = null;
            RenderInlineDiffs();
            if (NewText == null || OldText == null)
            {
                sideBySideResult = null;
                RenderSideBySideDiffs();
                return;
            }

            sideBySideResult = SideBySideDiffBuilder.Diff(OldText, NewText, IgnoreWhiteSpace, IgnoreCase);
            RenderSideBySideDiffs();
        }

        /// <summary>
        /// Switches to the view of side-by-side diff mode.
        /// </summary>
        public void ShowSideBySide()
        {
            IsSideBySide = true;
        }

        /// <summary>
        /// Switches to the view of inline diff mode.
        /// </summary>
        public void ShowInline()
        {
            IsSideBySide = false;
        }

        private void ChangeViewMode(bool isSideBySide)
        {
            InlineContentPanel.Visibility = InlineHeaderText.Visibility
                = (isSideBySide ? Visibility.Collapsed : Visibility.Visible);
            LeftContentPanel.Visibility = RightContentPanel.Visibility = LeftHeaderText.Visibility = RightHeaderText.Visibility = Splitter.Visibility
                = (isSideBySide ? Visibility.Visible : Visibility.Collapsed);

            if (isSideBySide)
                GetSideBySideDiffModel();
            else
                GetInlineDiffModel();

            ViewModeChanged?.Invoke(this, new ViewModeChangedEventArgs(isSideBySide));
        }

        /// <summary>
        /// Goes to a specific line.
        /// </summary>
        /// <param name="lineIndex">The index of the line to go to.</param>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <returns>true if it has turned to the specific line; otherwise, false.</returns>
        public bool GoTo(int lineIndex, bool isLeftLine = false)
        {
            if (IsSideBySideViewMode) return Helper.GoTo(isLeftLine ? LeftContentPanel : RightContentPanel, lineIndex);
            else return Helper.GoTo(InlineContentPanel, lineIndex);
        }

        /// <summary>
        /// Goes to a specific line.
        /// </summary>
        /// <param name="line">The line to go to.</param>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <returns>true if it has turned to the specific line; otherwise, false.</returns>
        public bool GoTo(DiffPiece line, bool isLeftLine = false)
        {
            if (IsSideBySideViewMode) return Helper.GoTo(isLeftLine ? LeftContentPanel : RightContentPanel, line);
            else return Helper.GoTo(InlineContentPanel, line);
        }

        /// <summary>
        /// Gets the line diff information.
        /// </summary>
        /// <param name="lineIndex">The index of the line to get information.</param>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <returns>The line diff information instance; or null, if non-exists.</returns>
        public DiffPiece GetLine(int lineIndex, bool isLeftLine = false)
        {
            if (IsSideBySideViewMode) return Helper.GetLine(isLeftLine ? LeftContentPanel : RightContentPanel, lineIndex);
            else return Helper.GetLine(InlineContentPanel, lineIndex);
        }

        /// <summary>
        /// Gets all line information in viewport.
        /// </summary>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesInViewport(bool isLeftLine = false, VisibilityLevels level = VisibilityLevels.Any)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesInViewport(isLeftLine ? LeftContentPanel : RightContentPanel, level);
            else return Helper.GetLinesInViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Gets all line information in viewport.
        /// </summary>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesInViewport(VisibilityLevels level)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesInViewport(RightContentPanel, level);
            else return Helper.GetLinesInViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Gets all line information before viewport.
        /// </summary>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesBeforeViewport(bool isLeftLine = false, VisibilityLevels level = VisibilityLevels.Any)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesBeforeViewport(isLeftLine ? LeftContentPanel : RightContentPanel, level);
            else return Helper.GetLinesBeforeViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Gets all line information before viewport.
        /// </summary>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesBeforeViewport(VisibilityLevels level)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesBeforeViewport(RightContentPanel, level);
            else return Helper.GetLinesBeforeViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Gets all line information after viewport.
        /// </summary>
        /// <param name="isLeftLine">true if goes to the line of the left panel for side-by-side (splitted) view; otherwise, false. This will be ignored when it is in inline view.</param>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesAfterViewport(bool isLeftLine = false, VisibilityLevels level = VisibilityLevels.Any)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesAfterViewport(isLeftLine ? LeftContentPanel : RightContentPanel, level);
            else return Helper.GetLinesAfterViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Gets all line information after viewport.
        /// </summary>
        /// <param name="level">The optional visibility level.</param>
        /// <returns>All lines.</returns>
        public IEnumerable<DiffPiece> GetLinesAfterViewport(VisibilityLevels level)
        {
            if (IsSideBySideViewMode) return Helper.GetLinesAfterViewport(RightContentPanel, level);
            else return Helper.GetLinesAfterViewport(InlineContentPanel, level);
        }

        /// <summary>
        /// Opens the context menu for view mode selection.
        /// </summary>
        public void OpenViewModeContextMenu()
        {
            HeaderContextMenu.IsOpen = true;
        }

        /// <summary>
        /// Collapses unchanged sections.
        /// </summary>
        /// <param name="contextLineCount">The optional context line count to set.</param>
        /// <exception cref="ArgumentOutOfRangeException">contextLineCount was less than 0.</exception>
        public void CollapseUnchangedSections(int? contextLineCount = null)
        {
            if (contextLineCount.HasValue)
            {
                if (contextLineCount.Value >= 0)
                {
                    LinesContext = contextLineCount.Value;
                }
                else if (contextLineCount.Value == -1)
                {
                    IgnoreUnchanged = false;
                    return;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(contextLineCount), "contextLineCount should be a natural integer.");
                }
            }

            IgnoreUnchanged = true;
        }

        /// <summary>
        /// Expands unchanged sections.
        /// </summary>
        public void ExpandUnchangedSections()
        {
            IgnoreUnchanged = false;
        }

        /// <summary>
        /// Sets header as old and new.
        /// </summary>
        public void SetHeaderAsOldToNew()
        {
            OldTextHeader = Resource.Old;
            NewTextHeader = Resource.New;
        }

        /// <summary>
        /// Sets header as left and right.
        /// </summary>
        public void SetHeaderAsLeftToRight()
        {
            OldTextHeader = Resource.Left;
            NewTextHeader = Resource.Right;
        }

        private void ContextLineMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem m) || !(m.Header is string s) || !int.TryParse(s.Replace("_", string.Empty), out var i) || i < 0) return;
            LinesContext = i;
        }

        /// <summary>
        /// Updates the side-by-side diffs view.
        /// </summary>
        private void RenderSideBySideDiffs()
        {
            LeftContentPanel.Clear();
            RightContentPanel.Clear();
            var m = sideBySideResult;
            CollapseUnchangedSectionsToggle.IsChecked = IgnoreUnchanged;
            if (m == null) return;
            var contextLineCount = IgnoreUnchanged ? LinesContext: -1;
            Helper.InsertLines(LeftContentPanel, m.OldText?.Lines, true, this, contextLineCount);
            Helper.InsertLines(RightContentPanel, m.NewText.Lines, false, this, contextLineCount);
        }

        /// <summary>
        /// Updates the inline diffs view.
        /// </summary>
        private void RenderInlineDiffs()
        {
            if (inlineResult?.Lines == null) return;
            ICollection<DiffPiece> selectedLines = inlineResult.Lines;
            Helper.RenderInlineDiffs(InlineContentPanel, selectedLines, this, IgnoreUnchanged ? LinesContext : -1);
        }

        private void LeftContentPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var offset = LeftContentPanel.VerticalOffset;
            if (Math.Abs(RightContentPanel.VerticalOffset - offset) > 1)
                RightContentPanel.ScrollToVerticalOffset(offset);
        }

        private void RightContentPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var offset = RightContentPanel.VerticalOffset;
            if (Math.Abs(LeftContentPanel.VerticalOffset - offset) > 1)
                LeftContentPanel.ScrollToVerticalOffset(offset);
        }

        private void ApplyHeaderTextProperties(TextBlock text)
        {
            text.SetBinding(TextBlock.FontSizeProperty, new Binding("FontSize") { Source = this, Mode = BindingMode.OneWay });
            text.SetBinding(TextBlock.FontFamilyProperty, new Binding("FontFamily") { Source = this, Mode = BindingMode.OneWay });
            text.SetBinding(TextBlock.FontWeightProperty, new Binding("FontWeight") { Source = this, Mode = BindingMode.OneWay });
            text.SetBinding(TextBlock.FontStretchProperty, new Binding("FontStretch") { Source = this, Mode = BindingMode.OneWay });
            text.SetBinding(TextBlock.FontStyleProperty, new Binding("FontStyle") { Source = this, Mode = BindingMode.OneWay });
            text.SetBinding(TextBlock.ForegroundProperty, new Binding("HeaderForeground") { Source = this, Mode = BindingMode.OneWay, TargetNullValue = Foreground });
        }

        private void UpdateHeaderText()
        {
            LeftHeaderText.Text = OldTextHeader;
            RightHeaderText.Text = NewTextHeader;
            if (string.IsNullOrEmpty(OldTextHeader) && string.IsNullOrEmpty(NewTextHeader))
            {
                InlineHeaderText.Text = null;
                return;
            }

            InlineHeaderText.Text = $"{OldTextHeader ?? string.Empty} → {NewTextHeader ?? string.Empty}";
            if (isHeaderEnabled) return;
            HeaderHeight = 20;
        }

        private void InlineModeToggle_Click(object sender, RoutedEventArgs e)
        {
            IsSideBySide = false;
        }

        private void SideBySideModeToggle_Click(object sender, RoutedEventArgs e)
        {
            IsSideBySide = true;
        }

        private void CollapseUnchangedSectionsToggle_Click(object sender, RoutedEventArgs e)
        {
            IgnoreUnchanged = !IgnoreUnchanged;
        }

        private void RefreshContextLinesMenuItemState(int i)
        {
            if (i > 9)
            {
                CustomizedContextLineMenuItem.Header = i.ToString("g");
                CustomizedContextLineMenuItem.Visibility = Visibility.Visible;
                i = 10;
            }
            else
            {
                CustomizedContextLineMenuItem.Visibility = Visibility.Collapsed;
            }

            var j = -1;
            foreach (var menu in ContextLinesMenuItems.Items)
            {
                j++;
                if (menu is MenuItem mi) mi.IsChecked = i == j;
            }
        }

        private static DependencyProperty RegisterDependencyProperty<T>(string name)
        {
            return DependencyProperty.Register(name, typeof(T), typeof(DiffViewer), null);
        }

        private static DependencyProperty RegisterDependencyProperty<T>(string name, T defaultValue, PropertyChangedCallback propertyChangedCallback = null)
        {
            return DependencyProperty.Register(name, typeof(T), typeof(DiffViewer), new PropertyMetadata(defaultValue, propertyChangedCallback));
        }

        private static DependencyProperty RegisterRefreshDependencyProperty<T>(string name, T defaultValue)
        {
            return DependencyProperty.Register(name, typeof(T), typeof(DiffViewer), new PropertyMetadata(defaultValue, (d, e) =>
            {
                if (!(d is DiffViewer c) || e.OldValue == e.NewValue) return;
                c.Refresh();
            }));
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (Keyboard.IsKeyDown(Key.Z))
                {
                    Undo();
                }
                else if (Keyboard.IsKeyDown(Key.Y))
                {
                    Redo();
                }
            }

        }
    }
}
