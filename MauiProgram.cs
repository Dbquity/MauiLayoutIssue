namespace LayoutIssue;
public static class MauiProgram {
    public static MauiApp CreateMauiApp() =>
        MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}
class App : Application {
    public App() {
        MainPage = new NavigationPage(
            new PageWithCustomLayout { Title = "Page with Custom Layout" });
    }
}
class PageWithCustomLayout : ContentPage {
    public PageWithCustomLayout() {
        const string LookupGlyph = "⌃";
        var layout = new CustomLayout {
            Margin = 20, Spacing = 20, MinColumnWidth = 160, MaxLeadWidth = 70 };
        Content = new VerticalStackLayout { layout };
        Button lookup;
        layout.Add(
            new Field(
                new Label { Text = "Problem:" },
                new Label { Text = "Some laid out controls get cut" }),
            new Field(
                new Label { Text = "Trigger:" },
                new Entry { Placeholder = $"<hit the [{LookupGlyph}]>" },
                lookup = new Button { Text = LookupGlyph }),
            new Field(
                new Label { Text = "Decide, pls:" },
                new Picker { ItemsSource = new[] { "this", "that" } }));
        lookup.Clicked +=
            async (_, __) => await Navigation.PushAsync(
                new LookupPage { Title = "Seek, and ye shall find" });
    }
}
class LookupPage : ContentPage {
    public LookupPage() {
        Content = new Label { HorizontalOptions = LayoutOptions.Center,
            Text = "I was looked up 🧐" + Environment.NewLine +
                   "  ... now please hit [<-] above" };
    }
}