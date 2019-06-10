using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NotepadEditorUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        List<string> colorList = new List<string>();    // holds the Windows.UI.Color names
        const int MIDDLE = 382;    // middle sum of RGB - max is 765
        int sumRGB;    // sum of the selected colors RGB
        //int pos, line, column;    // for detecting line and column numbers
        //int selectedFontSize = 0;
        //string filenamee;    // file opened inside of RTB
        public MainPage()
        {
            this.InitializeComponent();

            // Initialize MenuFlyouts on startup with the appropriate MenuFlyout object
            MenuFlyout mnuSize = mnuFontSize;
            MenuFlyout mnuColor = mnuFontColor;

            // Fill Font Menu Flyout Items List
            string[] fonts = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
            // Sort fonts a->z
            Array.Sort(fonts, (x, y) => String.Compare(x.ToString(), y.ToString()));
            foreach (string font in fonts)
            {
                MenuFlyoutItem mnFont = new MenuFlyoutItem();
                mnFont.Text = font;
                mnuFontSelection.Items.Add(mnFont);
                mnFont.Click += MnFont_Click;
            }

            // Fill Font Size Menu Flyout Items List
            for (int i = 0; i < 90; i++)
            {
                MenuFlyoutItem mnFontSize = new MenuFlyoutItem();
                mnFontSize.Text = i.ToString();
                mnuSize.Items.Add(mnFontSize);
                mnFontSize.Click += MnFontSize_Click;
            }

            // FontColor list
            foreach (PropertyInfo prop in typeof(Colors).GetProperties())
            {
                if (prop.PropertyType.FullName == "Windows.UI.Color")
                {
                    colorList.Add(prop.Name);
                }
            }

            // Fill FontColor Menu Flyout Items List
            foreach (string color in colorList)
            {
                MenuFlyoutItem mnFontColor = new MenuFlyoutItem();
                mnFontColor.Text = color;
                mnuColor.Items.Add(mnFontColor);
                mnFontColor.Click += MnFontColor_Click;
            }

            // Fill BackColor for each color in the Font Color Menu Flyout
            for (int i = 0; i < mnuColor.Items.Count; i++)
            {
                // Create Color object
                Color selectedColor = new Color();
                selectedColor = (Color)typeof(Colors).GetProperty(colorList[i]).GetValue(selectedColor);

                // Set background color of menu items according to its color
                mnuColor.Items[i].Background = new SolidColorBrush(selectedColor);

                // Set the text color depending on if the barkground is darker or lighter
                // Create Color object
                Color col = Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B);

                // 255,255,255 = White and 0,0,0 = Black
                // Max sum of RGB values is 765 -> (255 + 255 + 255)
                // Middle sum of RGB values is 382 -> (765/2)
                // Color is considered darker if its <= 382
                // Color is considered lighter if its > 382
                sumRGB = ConvertToRGB(col);    // get the color objects sum of the RGB value
                if (sumRGB <= MIDDLE)          // Darker Background
                {
                    mnuColor.Items[i].Foreground = new SolidColorBrush(Colors.White); // Set to white text
                }
                else if (sumRGB > MIDDLE)     // Lighter Background
                {
                    mnuColor.Items[i].Foreground = new SolidColorBrush(Colors.Black); // Set to black text
                }
            }

            // Fill FontStyle MenuFlyoutItem List
            Windows.UI.Text.ParagraphStyle[] styles;
            styles = new Windows.UI.Text.ParagraphStyle[4];
            styles[0] = Windows.UI.Text.ParagraphStyle.Heading1;
            styles[1] = Windows.UI.Text.ParagraphStyle.None;
            styles[2] = Windows.UI.Text.ParagraphStyle.Normal;
            styles[3] = Windows.UI.Text.ParagraphStyle.Undefined;
            for (int i = 0; i < styles.Count(); i++)
            {
                MenuFlyoutItem mnFontStyles = new MenuFlyoutItem();
                mnFontStyles.Text = styles[i].ToString();
                mnuFontStyles.Items.Add(mnFontStyles);
                mnFontStyles.Click += MnFontStyles_Click;
            }
        }

        //***************************************************************
        // RichEditBox MenuFlyoutItem Click Events for different menus
        //***************************************************************
        private void MnFontStyles_Click(object sender, RoutedEventArgs e)
        {
            // Set the editors font according to the MenuFlyoutItem selected
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            Windows.UI.Text.ParagraphStyle style = new Windows.UI.Text.ParagraphStyle();
            if (item.Text == "Heading1")
                style = Windows.UI.Text.ParagraphStyle.Heading1;
            else if (item.Text == "None")
                style = Windows.UI.Text.ParagraphStyle.None;
            else if (item.Text == "Normal")
                style = Windows.UI.Text.ParagraphStyle.Normal;
            else if (item.Text == "Undefined")
                style = Windows.UI.Text.ParagraphStyle.Undefined;
            editor.Document.Selection.ParagraphFormat.Style = style;
        }

        private void MnFont_Click(object sender, RoutedEventArgs e)
        {
            // Set the editors font according to the MenuFlyoutItem selected
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            FontFamily selectedFont = new FontFamily(item.Text);
            editor.Document.Selection.CharacterFormat.Name = item.Text;
        }

        private void MnFontColor_Click(object sender, RoutedEventArgs e)
        {
            // Set the editors font color according to the MenuFlyoutItem selected
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            Color selectedColor = new Color();
            selectedColor = (Color)typeof(Colors).GetProperty(item.Text).GetValue(selectedColor);
            editor.Document.Selection.CharacterFormat.ForegroundColor = selectedColor;
        }

        private void MnFontSize_Click(object sender, RoutedEventArgs e)
        {
            // Set the editors font size according to the MenuFlyoutItem selected
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            editor.Document.Selection.CharacterFormat.Size = Int32.Parse(item.Text);
        }

        //******************************************************************************************************************************
        // ConvertToRGB - Accepts a Color object as its parameter. Gets the RGB values of the object passed to it, calculates the sum. *
        //******************************************************************************************************************************
        private int ConvertToRGB(Windows.UI.Color c)
        {
            int r = c.R, // RED component value
                g = c.G, // GREEN component value
                b = c.B; // BLUE component value
            int sum = 0;

            // calculate sum of RGB
            sum = r + g + b;

            return sum;
        }

        //**************************************************************
        // RichEditBox Font Style AppBarToggleButton Checked Events
        //**************************************************************
        private void btnBold_Checked(object sender, RoutedEventArgs e)
        {
            // Enable Bold Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;

            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void btnItalic_Checked(object sender, RoutedEventArgs e)
        {
            // Enable Italic Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void btnUnderline_Checked(object sender, RoutedEventArgs e)
        {
            // Enable Underlined Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                if (charFormatting.Underline == Windows.UI.Text.UnderlineType.None)
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.Single;
                }
                else
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.None;
                }
                selectedText.CharacterFormat = charFormatting;
            }
        }

        //**************************************************************
        // RichEditBox Font Style AppBarToggleButton Unchecked Events
        //**************************************************************
        private void btnBold_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable Bold Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;

            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void btnItalic_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable Italic Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void btnUnderline_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable Underlined Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                if (charFormatting.Underline == Windows.UI.Text.UnderlineType.None)
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.Single;
                }
                else
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.None;
                }
                selectedText.CharacterFormat = charFormatting;
            }
        }

        //**********************************************************
        // RichEditBox Undo/Redo Button Click Events
        //**********************************************************
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Undo();
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Redo();
        }

        //*********************************************************************************************
        // editor_DragEnter - Custom Event. Copies text being dragged into the richTextBox      
        //*********************************************************************************************
        private void editor_DragOver(object sender, DragEventArgs e)
        {
            var point = e.GetPosition(editor);
            var range = editor.Document.GetRangeFromPoint(point, Windows.UI.Text.PointOptions.ClientCoordinates);
            editor.Document.Selection.SetRange(range.StartPosition - 1, range.EndPosition);
            editor.Focus(FocusState.Keyboard);
        }

        //***************************************************************************************************
        // editor_DragEnter - Custom Event. Drops the copied text being dragged onto the richTextBox  
        //***************************************************************************************************
        private async void editor_DragEnter(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(this, "Outside", true);
            bool hasText = e.DataView.Contains(StandardDataFormats.Text);
            // if the result of the drop is not too important (and a text copy should have no impact on source)
            // we don't need to take the deferral and this will complete the operation faster
            e.AcceptedOperation = hasText ? DataPackageOperation.Copy : DataPackageOperation.None;
            if (hasText)
            {
                var text = await e.DataView.GetTextAsync();
                editor.Document.Selection.Text = text;
            }
        }

        //******************************************************************
        // Fullscreen AppBarToggleButton Checked Events
        //******************************************************************
        //private void btnFullscreen_Checked(object sender, RoutedEventArgs e)
        //{
        //    // Enter Fullscreen Mode
        //    ApplicationView view = ApplicationView.GetForCurrentView();
        //    view.TryEnterFullScreenMode();
        //}

        //private void btnFullscreen_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    // Exit Fullscreen mode
        //    ApplicationView view = ApplicationView.GetForCurrentView();
        //    view.ExitFullScreenMode();        
        //}

        //****************************************************************
        // RichEditBox Open/Save File Button Click Events
        //****************************************************************
        private async void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            //// Open a text file.
            //Windows.Storage.Pickers.FileOpenPicker open =
            //    new Windows.Storage.Pickers.FileOpenPicker();
            //open.SuggestedStartLocation =
            //    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            //open.FileTypeFilter.Add(".rtf");
            //open.FileTypeFilter.Add(".txt");

            //Windows.Storage.StorageFile file = await open.PickSingleFileAsync();
            //if (file != null)
            //{
            //    try
            //    {
            //        Windows.Storage.Streams.IRandomAccessStream randAccStream =
            //    await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

            //        //// Load the file into the Document property of the RichEditBox.
            //        if (file.FileType == ".rtf")
            //            editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream);
            //        else if(file.FileType == ".txt")
            //            editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.None, randAccStream);
            //    }
            //    catch (Exception)
            //    {
            //        ContentDialog errorDialog = new ContentDialog()
            //        {
            //            Title = "File open error",
            //            Content = "Sorry, I couldn't open the file.",
            //            PrimaryButtonText = "Ok"
            //        };

            //        await errorDialog.ShowAsync();
            //    }
            //}

            // Open a text file.
            Windows.Storage.Pickers.FileOpenPicker open =
                new Windows.Storage.Pickers.FileOpenPicker();
            open.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".rtf");
            open.FileTypeFilter.Add(".txt");
            open.FileTypeFilter.Add(".docx");
            Windows.Storage.StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    var reader = DataReader.FromBuffer(buffer);
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string text = reader.ReadString(buffer.Length);
                    //// Load the file into the Document property of the RichEditBox.
                    editor.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, text);
                }
                catch (Exception)
                {
                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "File open error",
                        Content = "Sorry, I couldn't open the file.",
                        PrimaryButtonText = "Ok"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // SaveFile Picker
            Windows.Storage.Pickers.FileSavePicker savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text Format", new List<string>() { ".rtf" });
            savePicker.FileTypeChoices.Add("Normal Text File", new List<string>() { ".txt" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // write to file
                Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                if (file.FileType == ".rtf")
                    editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                else if (file.FileType == ".txt")
                    editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.None, randAccStream);

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }

        //*****************************************************************
        // RichEditBox Text Alignment AppBarToggleButton Checked Events
        //*****************************************************************
        private void btnAlignLeft_Checked(object sender, RoutedEventArgs e)
        {
            // Left Align Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
                selectedText.ParagraphFormat = paraFormatting;
            }
            btnAlignCenter.IsChecked = false;
            btnAlignRight.IsChecked = false;
        }

        private void btnAlignCenter_Checked(object sender, RoutedEventArgs e)
        {
            // Center Align Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Center;
                selectedText.ParagraphFormat = paraFormatting;
            }
            btnAlignLeft.IsChecked = false;
            btnAlignRight.IsChecked = false;
        }

        private void btnAlignRight_Checked(object sender, RoutedEventArgs e)
        {
            // Enable Right Align Text
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Right;
                selectedText.ParagraphFormat = paraFormatting;
            }
            btnAlignLeft.IsChecked = false;
            btnAlignCenter.IsChecked = false;
        }

        // Find Simpler way at some point *****
        //*****************************************************************
        // RichEditBox Text Alignment AppBarToggleButton Unchecked Events
        //*****************************************************************
        private void btnAlignLeft_Unchecked(object sender, RoutedEventArgs e)
        {
            //// Disable Left Align Text
            //Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            //if (selectedText != null)
            //{
            //    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
            //    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
            //    selectedText.ParagraphFormat = paraFormatting;
            //}
            //btnAlignLeft.IsChecked = false;
            //btnAlignCenter.IsChecked = false;
        }

        private void btnAlignCenter_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable Center Align Text
            //Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            //if (selectedText != null)
            //{
            //    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
            //    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
            //    selectedText.ParagraphFormat = paraFormatting;
            //}
            //btnAlignLeft.IsChecked = false;
            //btnAlignCenter.IsChecked = false;

            if (btnAlignRight.IsChecked == true)
            {
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Right;
                    selectedText.ParagraphFormat = paraFormatting;
                }
                btnAlignLeft.IsChecked = false;
                btnAlignCenter.IsChecked = false;
            }
            else if (btnAlignLeft.IsChecked == true)
            {
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
                    selectedText.ParagraphFormat = paraFormatting;
                }
                btnAlignCenter.IsChecked = false;
                btnAlignRight.IsChecked = false;
            }
            else
            {
                // Disable Center Align Text
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
                    selectedText.ParagraphFormat = paraFormatting;
                }
            }
        }

        private void btnAlignRight_Unchecked(object sender, RoutedEventArgs e)
        {
            //// Disable Right Align Text
            //Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            //if (selectedText != null)
            //{
            //    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
            //    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
            //    selectedText.ParagraphFormat = paraFormatting;
            //}
            //btnAlignLeft.IsChecked = false;
            //btnAlignCenter.IsChecked = false;
            if (btnAlignCenter.IsChecked == true)
            {
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Center;
                    selectedText.ParagraphFormat = paraFormatting;
                }
                btnAlignLeft.IsChecked = false;
                btnAlignRight.IsChecked = false;
            }
            else if (btnAlignLeft.IsChecked == true)
            {
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
                    selectedText.ParagraphFormat = paraFormatting;
                }
                btnAlignCenter.IsChecked = false;
                btnAlignRight.IsChecked = false;
            }
            else
            {
                // Disable Center Align Text
                Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                    paraFormatting.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
                    selectedText.ParagraphFormat = paraFormatting;
                }
            }
        }

        //*******************************************************************
        // RichEditBox Font Increase/Decrease Button Click Events
        //*******************************************************************
        private void btnFontIncrease_Click(object sender, RoutedEventArgs e)
        {
            // Increase font size by one each time button is clicked
            float currentSize = editor.Document.Selection.CharacterFormat.Size;
            editor.Document.Selection.CharacterFormat.Size += 1;
        }

        private void btnFontDecrease_Click(object sender, RoutedEventArgs e)
        {
            // Decrease font size by one each time button is clicked
            float currentSize = editor.Document.Selection.CharacterFormat.Size;
            editor.Document.Selection.CharacterFormat.Size -= 1;
        }

        //**********************************************************
        // RichEditBox Copy/Cut/Paste/Clear Button Click Events
        //**********************************************************
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Selection.Copy();
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Selection.Cut();
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Selection.Paste(0);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            string text = "";
            editor.Document.GetText(Windows.UI.Text.TextGetOptions.None, out text);
            ITextRange allText = editor.Document.GetRange(0, text.Length - 1);
            if (allText != null)
            {
                // Reset Character Formats
                ITextCharacterFormat charFormatting = allText.CharacterFormat;
                charFormatting.Name = "Segoe UI";
                charFormatting.Size = 15f;
                charFormatting.ForegroundColor = Colors.Black;
                charFormatting.Bold = charFormatting.Bold = FormatEffect.Off;
                charFormatting.Italic = charFormatting.Bold = FormatEffect.Off;
                charFormatting.Underline = Windows.UI.Text.UnderlineType.None;
                allText.CharacterFormat = charFormatting;

                // Reset Paragraph Formats and Style
                ITextParagraphFormat paraFormatting = allText.ParagraphFormat;
                paraFormatting.ListType = MarkerType.None;
                //paraFormatting.Style = ParagraphStyle.Undefined;
                allText.ParagraphFormat = paraFormatting;
            }
        }

        //***************************************************************
        // MenuFlyoutItem Click Evevts for mnuBullets to select from
        //***************************************************************
        private void mnuItmBullet_Click(object sender, RoutedEventArgs e)
        {
            // Bullet List
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.Bullet;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItmCircleNumber_Click(object sender, RoutedEventArgs e)
        {
            // Circled Number List
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.CircledNumber;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItemUpperEngLetter_Click(object sender, RoutedEventArgs e)
        {
            // Uppercase English Letter List - Ex: A), B), C)...
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.UppercaseEnglishLetter;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItmLowerEngLetter_Click(object sender, RoutedEventArgs e)
        {
            // Lowercase English Letter List - Ex: a), b), c)...
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.LowercaseEnglishLetter;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItmUpperRomanNumeral_Click(object sender, RoutedEventArgs e)
        {
            // Uppercase Roman Numeral List - Ex: I), II), III)...
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.UppercaseRoman;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItmLowerRomanNumeral_Click(object sender, RoutedEventArgs e)
        {
            // Lowercase Roman Numeral List - Ex: i), ii), iii)...
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.LowercaseRoman;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        private void mnuItmNone_Click(object sender, RoutedEventArgs e)
        {
            // No Bullet List
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextParagraphFormat paraFormatting = selectedText.ParagraphFormat;
                paraFormatting.ListType = Windows.UI.Text.MarkerType.None;
                selectedText.ParagraphFormat = paraFormatting;
            }
        }

        //************************************************************************
        // RichEditBox Case Change Button Click Events
        //************************************************************************
        private void mnuItemChangeCaseLower_Click(object sender, RoutedEventArgs e)
        {
            // Change case to lower
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Selection.ChangeCase(Windows.UI.Text.LetterCase.Lower);
        }

        private void mnuItemChangeCaseUpper_Click(object sender, RoutedEventArgs e)
        {
            // Change case to upper
            Windows.UI.Text.ITextDocument doc = editor.Document;
            doc.Selection.ChangeCase(Windows.UI.Text.LetterCase.Upper);
        }

    }
}
