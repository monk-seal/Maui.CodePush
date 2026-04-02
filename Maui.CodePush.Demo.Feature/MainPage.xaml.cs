namespace Maui.CodePush.Demo.Feature;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;
        CounterLabel.Text = count.ToString();
        StatusLabel.Text = count == 1
            ? "You tapped 1 time!"
            : $"You tapped {count} times!";

        // Visual feedback - pulse animation
        CounterLabel.ScaleTo(1.3, 100)
            .ContinueWith(_ => CounterLabel.ScaleTo(1.0, 100));

        CounterBtn.Text = $"TAPPED {count}x - KEEP GOING!";
    }
}
