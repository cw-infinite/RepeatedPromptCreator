using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PromptStudio {
public class PromptItem {
    public string Name { get; set; }
    public string Template { get; set; }
    public Dictionary<string,string> Values { get; set; }
    public PromptItem() { Name="Untitled template"; Template=""; Values=new Dictionary<string,string>(); }
}
public class Workspace {
    public List<PromptItem> Items { get; set; }
    public int Selected { get; set; }
    public bool Dark { get; set; }
}
public static class Engine {
    public static readonly Regex Pattern = new Regex(@"\{\{\s*([\p{L}\p{N}_][\p{L}\p{N}_ \-]*?)\s*\}\}");
    public static List<string> Names(string template) { return Pattern.Matches(template).Cast<Match>().Select(m=>m.Groups[1].Value.Trim()).Distinct().ToList(); }
    public static string Render(string template, Dictionary<string,string> values) { return Pattern.Replace(template,m=> { string value; return values.TryGetValue(m.Groups[1].Value.Trim(),out value) && !string.IsNullOrWhiteSpace(value) ? value : m.Value; }); }
}
public class MainWindow : Window {
    Workspace state; PromptItem current; bool loading; bool rebuilding;
    TextBox title, editor, output, variableName; ComboBox library; StackPanel inputs; TextBlock status, count, inputCount; Button theme;
    DispatcherTimer saveTimer = new DispatcherTimer();
    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PromptStudio");
    public MainWindow() {
        Title="Prompt Studio"; Width=1480; Height=940; MinWidth=1080; MinHeight=720; WindowStartupLocation=WindowStartupLocation.CenterScreen;
        FontFamily=new FontFamily("Segoe UI"); FontSize=16;
        state=Load(); Build(); ApplyTheme(); Select(Math.Max(0,Math.Min(state.Selected,state.Items.Count-1)));
        saveTimer.Interval=TimeSpan.FromMilliseconds(600); saveTimer.Tick+=(s,e)=>{saveTimer.Stop(); Save();};
        Closing+=(s,e)=>Save();
        PreviewKeyDown+=(s,e)=> { if(Keyboard.Modifiers==(ModifierKeys.Control|ModifierKeys.Shift) && e.Key==Key.C){Copy();e.Handled=true;} else if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.N){New();e.Handled=true;} };
    }
    Workspace Load() {
        try { var path=Path.Combine(folder,"workspace.json"); if(File.Exists(path)) { var loaded=new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Workspace>(File.ReadAllText(path)); if(loaded!=null && loaded.Items!=null && loaded.Items.Count>0) { foreach(var item in loaded.Items){if(item.Values==null)item.Values=new Dictionary<string,string>();if(item.Template==null)item.Template="";} return loaded; } } } catch { MessageBox.Show("Your saved workspace could not be loaded. A recovery copy will be kept before saving.","Prompt Studio"); try { File.Copy(Path.Combine(folder,"workspace.json"),Path.Combine(folder,"workspace.recovery-"+DateTime.Now.ToString("yyyyMMddHHmmss")+".json")); } catch {} }
        return new Workspace { Items=new List<PromptItem> {
            new PromptItem { Name="Content brief", Template="You are an experienced {{role}}.\n\nCreate a clear, useful response about {{topic}} for {{audience}}.\n\nUse this source material:\n{{source_text}}\n\nRequirements:\n• Write in a {{tone}} tone.\n• Structure the response as {{format}}.\n• Stay grounded in the source material.\n• If something is unclear, explain what information is missing.", Values=new Dictionary<string,string>{{"role","content strategist"},{"topic","a new community garden"},{"audience","local residents"},{"source_text","The garden opens on Saturday. Volunteers can join weekly planting sessions. All experience levels are welcome."},{"tone","warm and approachable"},{"format","a short introduction and three actionable bullet points"}} },
            new PromptItem { Name="Summarize a document", Template="Summarize the document below for {{audience}}.\n\nDocument:\n{{document}}\n\nReturn:\n1. A short overview\n2. The key findings\n3. Recommended next steps\n\nKeep the summary under {{word_count}} words.", Values=new Dictionary<string,string>{{"audience","a busy team lead"},{"word_count","250"}} },
            new PromptItem { Name="Review my writing", Template="Act as a thoughtful editor. Review the following text:\n\n{{draft}}\n\nMy goal: {{goal}}\nPreferred tone: {{tone}}\n\nFirst give specific suggestions, then provide a revised version that preserves my meaning." }
        }};
    }
    void Save() { try { Directory.CreateDirectory(folder); string path=Path.Combine(folder,"workspace.json"); string temp=path+".tmp"; File.WriteAllText(temp,new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(state)); if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path); status.Text="Saved on this device  ·  No cloud connection"; } catch(Exception ex) {status.Text="Could not save: "+ex.Message;} }
    void ScheduleSave(){if(loading)return;saveTimer.Stop();saveTimer.Start();status.Text="Saving changes…";}
    SolidColorBrush Brush(string hex){return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);}
    void ApplyTheme() {
        bool d=state.Dark;
        Resources["Bg"]=Brush(d?"#11151D":"#F3F5FA"); Resources["Card"]=Brush(d?"#1B2230":"#FFFFFF"); Resources["Field"]=Brush(d?"#131A26":"#F8FAFD"); Resources["Text"]=Brush(d?"#F0F3FA":"#182337"); Resources["Muted"]=Brush(d?"#AAB7CD":"#5D6B82"); Resources["Line"]=Brush(d?"#344158":"#DDE4EF"); Resources["Accent"]=Brush(d?"#A79BFF":"#6652D9");
        SetResourceReference(BackgroundProperty,"Bg"); SetResourceReference(ForegroundProperty,"Text"); theme.Content=d?"☀  Light theme":"☾  Dark theme";
    }
    TextBlock Label(string text,double size,string color){var t=new TextBlock{Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap};t.SetResourceReference(TextBlock.ForegroundProperty,color);return t;}
    Button Btn(string text,Action action,bool primary=false){var b=new Button{Content=text,Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,0,8,0),FontSize=16,Cursor=Cursors.Hand,MinHeight=44};b.SetResourceReference(Control.BackgroundProperty,primary?"Accent":"Field");b.SetResourceReference(Control.ForegroundProperty,primary?"Card":"Text");b.SetResourceReference(Control.BorderBrushProperty,"Line"); b.Click+=(s,e)=>action(); return b;}
    TextBox Field(bool multi) {var t=new TextBox { FontSize=17,Padding=new Thickness(13),BorderThickness=new Thickness(1),AcceptsReturn=multi,TextWrapping=multi?TextWrapping.Wrap:TextWrapping.NoWrap,VerticalScrollBarVisibility=multi?ScrollBarVisibility.Auto:ScrollBarVisibility.Hidden }; t.SetResourceReference(Control.BackgroundProperty,"Field");t.SetResourceReference(Control.ForegroundProperty,"Text");t.SetResourceReference(Control.BorderBrushProperty,"Line");t.SetResourceReference(TextBox.CaretBrushProperty,"Text");return t;}
    Border Card(UIElement child) {var b=new Border{CornerRadius=new CornerRadius(16),Padding=new Thickness(22),Margin=new Thickness(0,0,14,0),BorderThickness=new Thickness(1),Child=child};b.SetResourceReference(Border.BackgroundProperty,"Card");b.SetResourceReference(Border.BorderBrushProperty,"Line");return b;}
    void Build(){
        var root=new Grid{Margin=new Thickness(28)};root.SetResourceReference(Panel.BackgroundProperty,"Bg");Content=root; root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var top=new DockPanel{Margin=new Thickness(0,0,0,24)};Grid.SetRow(top,0);root.Children.Add(top);
        theme=Btn("",()=>{state.Dark=!state.Dark;ApplyTheme();ScheduleSave();});theme.MinWidth=170;DockPanel.SetDock(theme,Dock.Right);top.Children.Add(theme);
        var brand=new StackPanel();brand.Children.Add(Label("◈  Prompt Studio",30,"Text"));brand.Children.Add(Label("A little structure. A better prompt.",16,"Muted"));top.Children.Add(brand);
        var toolbar=new DockPanel{Margin=new Thickness(0,0,0,22)};Grid.SetRow(toolbar,1);root.Children.Add(toolbar);
        var actions=new StackPanel{Orientation=Orientation.Horizontal};actions.Children.Add(Btn("+ New",New));actions.Children.Add(Btn("Duplicate",Duplicate));actions.Children.Add(Btn("Delete",Delete));DockPanel.SetDock(actions,Dock.Right);toolbar.Children.Add(actions);
        library=new ComboBox{MinWidth=250,MaxWidth=400,HorizontalAlignment=HorizontalAlignment.Left,FontSize=17,Padding=new Thickness(12,8,12,8),Margin=new Thickness(0,0,20,0)};library.SelectionChanged+=(s,e)=>{if(!rebuilding && library.SelectedIndex>=0)Select(library.SelectedIndex);};toolbar.Children.Add(library);
        var columns=new Grid();Grid.SetRow(columns,2);root.Children.Add(columns);columns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.05,GridUnitType.Star)});columns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});columns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.12,GridUnitType.Star)});
        var left=new Grid();left.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});left.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});left.RowDefinitions.Add(new RowDefinition());left.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var lh=new StackPanel();lh.Children.Add(Label("01   BUILD YOUR TEMPLATE   →",13,"Accent"));lh.Children.Add(Label("The structure",24,"Text"));lh.Children.Add(new TextBlock{Height=14});title=Field(false);title.ToolTip="Template name";title.TextChanged+=(s,e)=>{if(loading)return;current.Name=title.Text;RefreshLibrary();ScheduleSave();};lh.Children.Add(title);left.Children.Add(lh);
        var hint=Label("Write your prompt. Use {{variable}} wherever you want a reusable input.",15,"Muted");hint.Margin=new Thickness(0,16,0,12);Grid.SetRow(hint,1);left.Children.Add(hint);
        editor=Field(true);editor.VerticalContentAlignment=VerticalAlignment.Top;editor.TextChanged+=(s,e)=>{if(loading)return;current.Template=editor.Text;RebuildInputs();Render();ScheduleSave();};Grid.SetRow(editor,2);left.Children.Add(editor);
        var add=new StackPanel{Margin=new Thickness(0,14,0,0)};add.Children.Add(Label("INSERT A VARIABLE",12,"Muted"));variableName=Field(false);variableName.Margin=new Thickness(0,8,0,8);variableName.ToolTip="Variable name, for example: source_text";variableName.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){AddVariable();e.Handled=true;}};add.Children.Add(variableName);add.Children.Add(Btn("+ Add variable",AddVariable));Grid.SetRow(add,3);left.Children.Add(add);columns.Children.Add(Card(left));
        var middle=new DockPanel();var mh=new StackPanel{Margin=new Thickness(0,0,0,18)};mh.Children.Add(Label("02   FILL IN THE DETAILS   →",13,"Accent"));mh.Children.Add(Label("Your inputs",24,"Text"));inputCount=Label("",15,"Muted");inputCount.Margin=new Thickness(0,8,0,0);mh.Children.Add(inputCount);DockPanel.SetDock(mh,Dock.Top);middle.Children.Add(mh);inputs=new StackPanel();middle.Children.Add(new ScrollViewer{Content=inputs,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,8,0)});var mc=Card(middle);Grid.SetColumn(mc,1);columns.Children.Add(mc);
        var right=new DockPanel();var rh=new StackPanel{Margin=new Thickness(0,0,0,18)};rh.Children.Add(Label("03   READY TO USE",13,"Accent"));rh.Children.Add(Label("Final prompt",24,"Text"));rh.Children.Add(Label("Live preview · updates as you type",15,"Muted"));DockPanel.SetDock(rh,Dock.Top);right.Children.Add(rh);
        var footer=new StackPanel{Margin=new Thickness(0,16,0,0)};count=Label("",14,"Muted");footer.Children.Add(count);var buttons=new WrapPanel{Margin=new Thickness(0,12,0,0)};buttons.Children.Add(Btn("Copy prompt",Copy,true));buttons.Children.Add(Btn("Export .txt",Export));footer.Children.Add(buttons);DockPanel.SetDock(footer,Dock.Bottom);right.Children.Add(footer);output=Field(true);output.IsReadOnly=true;right.Children.Add(output);var rc=Card(right);rc.Margin=new Thickness(0);Grid.SetColumn(rc,2);columns.Children.Add(rc);
        status=Label("Saved on this device  ·  No cloud connection",13,"Muted");status.Margin=new Thickness(0,16,0,0);Grid.SetRow(status,3);root.Children.Add(status);RefreshLibrary();
    }
    void RefreshLibrary(){rebuilding=true;library.ItemsSource=state.Items.Select(i=>string.IsNullOrWhiteSpace(i.Name)?"Untitled template":i.Name).ToList();library.SelectedIndex=state.Selected;rebuilding=false;}
    void Select(int index){loading=true;state.Selected=index;current=state.Items[index];title.Text=current.Name;editor.Text=current.Template;RefreshLibrary();RebuildInputs();Render();loading=false;ScheduleSave();}
    void RebuildInputs(){var names=Engine.Names(current.Template);var oldNames=inputs.Children.OfType<StackPanel>().Select(p=>(string)p.Tag).ToList();if(oldNames.SequenceEqual(names)&& !loading)return;inputs.Children.Clear();if(names.Count==0){inputs.Children.Add(Label("Your inputs will appear here.\n\nAdd a {{variable}} to your template to get started.",17,"Muted"));}foreach(var name in names){var panel=new StackPanel{Tag=name,Margin=new Thickness(0,0,0,20)};var label=Label(name.Replace('_',' '),17,"Text");label.FontWeight=FontWeights.SemiBold;panel.Children.Add(label);var token=Label("{{"+name+"}}",12,"Muted");token.Margin=new Thickness(0,3,0,8);panel.Children.Add(token);var field=Field(true);field.MinHeight=100;field.MaxHeight=220;string value;field.Text=current.Values.TryGetValue(name,out value)?value:"";field.ToolTip="Type or paste text for "+name;System.Windows.Automation.AutomationProperties.SetName(field,name);field.TextChanged+=(s,e)=>{current.Values[name]=field.Text;Render();ScheduleSave();};panel.Children.Add(field);inputs.Children.Add(panel);}}
    void Render(){output.Text=Engine.Render(current.Template,current.Values);var names=Engine.Names(current.Template);int filled=names.Count(n=>current.Values.ContainsKey(n)&&!string.IsNullOrWhiteSpace(current.Values[n]));inputCount.Text=names.Count==0?"No variables yet":filled+" of "+names.Count+" inputs filled";count.Text=output.Text.Length.ToString("N0")+" characters  ·  "+(names.Count-filled==0?"Ready to copy":(names.Count-filled)+" unfilled input(s)");}
    void AddVariable(){string name=variableName.Text.Trim();if(!Regex.IsMatch(name,@"^[\p{L}\p{N}_][\p{L}\p{N}_ \-]*$")){status.Text="Enter a variable name using letters, numbers, spaces, underscores, or hyphens.";variableName.Focus();return;}int pos=editor.SelectionStart;string token="{{"+name+"}}";editor.SelectedText=token;editor.Focus();editor.CaretIndex=pos+token.Length;variableName.Clear();}
    void New(){state.Items.Add(new PromptItem{Name="Untitled template",Template="Help me with {{task}}.\n\nContext:\n{{context}}\n\nReturn the result as {{format}}."});Select(state.Items.Count-1);title.Focus();title.SelectAll();}
    void Duplicate(){state.Items.Add(new PromptItem{Name=current.Name+" (copy)",Template=current.Template,Values=new Dictionary<string,string>(current.Values)});Select(state.Items.Count-1);}
    void Delete(){if(MessageBox.Show(this,"Delete “"+current.Name+"” and its saved inputs?","Delete template",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;int index=state.Selected;state.Items.RemoveAt(index);if(state.Items.Count==0)state.Items.Add(new PromptItem());Select(Math.Min(index,state.Items.Count-1));}
    void Copy(){try{Clipboard.SetText(output.Text);status.Text="Prompt copied  ·  Paste it into your favorite AI app";}catch{status.Text="Clipboard is busy. Try copying again.";}}
    void Export(){var dialog=new Microsoft.Win32.SaveFileDialog{Filter="Text file (*.txt)|*.txt",FileName="prompt.txt"};if(dialog.ShowDialog(this)==true){try{File.WriteAllText(dialog.FileName,output.Text);status.Text="Prompt exported successfully";}catch(Exception ex){status.Text="Could not export: "+ex.Message;}}}
}
public class Program {
    [STAThread] public static void Main(){var app=new Application();app.Run(new MainWindow());}
}
}

