using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBullet.Views.Main;

public partial class Help : Page
{
    private AboutPage aboutPage;
    public CheckUpdatePage CheckUpdatePage;

    public Help()
    {
        InitializeComponent();
        aboutPage      = new AboutPage();
        CheckUpdatePage = new CheckUpdatePage();
        Main.Content   = aboutPage;
        MenuSelect(aboutLabel);
    }

    private void MenuSelect(Label selected)
    {
        foreach (object child in topMenu.Children)
        {
            if (child is Label lbl)
                lbl.Foreground = Utils.GetBrush("ForegroundMain");
        }
        selected.Foreground = Utils.GetBrush("ForegroundCustom");
    }

    private void aboutLabel_Click(object sender, MouseButtonEventArgs e)
    {
        Main.Content = aboutPage;
        MenuSelect(aboutLabel);
    }

    public void checkForUpdateLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Main.Content = CheckUpdatePage;
        MenuSelect(checkForUpdateLabel);
    }
}
