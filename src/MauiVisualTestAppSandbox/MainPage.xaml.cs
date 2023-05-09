using System.Collections.ObjectModel;
using System.Reflection;
using Drastic.MauiRemoteImage.Client;
using Drastic.MauiRemoteImage.Messages;
using Drastic.MauiRemoteImage.Models;
using Drastic.Tempest;
using MauiVisualTestApp.Shared;

namespace MauiVisualTestAppSandbox;

public partial class MainPage : ContentPage
{
	private AppClient client;
	
	public MainPage()
	{
		InitializeComponent();
		this.BindingContext = this;
		this.SetupPages();
		var name = string.Empty;
		#if IOS
		name = "iOS";
		#elif ANDROID
		name = "Android";
		#elif MACCATALYST
		name = "MacCatalyst";
		#elif WINDOWS
		name = "Windows";
		#endif
		this.client = new AppClient(name);
		this.client.ConnectAsync(new Target("192.168.50.123", 8888));
	}

	private void SetupPages()
	{
		string ns = "MauiVisualTestApp.Shared";
		
		foreach (Type type in Assembly.GetAssembly(typeof(ButtonPage))!.GetTypes())
		{
			if (type.Namespace == ns && type.IsSubclassOf(typeof(Page)))
			{
				this.Pages.Add(new PageHolder() { Title = type.Name, PageType = type });
			}
		}
	}

	public ObservableCollection<PageHolder> Pages { get; private set; } = new ObservableCollection<PageHolder>();

	public class PageHolder
	{
		public string Title { get; set; }

		public Type PageType { get; set; }
	}
	
	private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
	{
		if (e.SelectedItem is not PageHolder holder)
		{
			return;
		}

		Page page = (Page)Activator.CreateInstance(holder.PageType);
		Navigation.PushAsync(page);
		((ListView)sender).SelectedItem = null;
	}
	
	private async void Button_OnClicked(object sender, EventArgs e)
	{
		var window = new Window(new StartingPage());
		App.Current.OpenWindow(window);
		await Task.Delay(1000);
		
		string ns = "MauiVisualTestApp.Shared";
		foreach (Type type in Assembly.GetAssembly(typeof(ButtonPage))!.GetTypes())
		{
			if (type.Namespace == ns && type.IsSubclassOf(typeof(Page)))
			{
				var eventFired = new ManualResetEventSlim(false);
				var timeout = TimeSpan.FromSeconds(10);
				
				var page = (Page)Activator.CreateInstance(type);
				
				EventHandler handler = (sender, args) =>
				{
					eventFired.Set();
				};
				
				page.Appearing += handler;
				window.Page = page;
				
				try
				{
					// Wait for the event to fire or the timeout to elapse
					if (!eventFired.Wait(timeout))
					{
						throw new TimeoutException("Event did not fire within the specified timeout.");
					}

					await Task.Delay(1000);
					var image = await VisualDiagnostics.CaptureAsPngAsync(window);
					var screenshot = new Drastic.MauiRemoteImage.Models.Screenshot() { Image = image, Name = type.Name };
					this.client.SendScreenshot(screenshot);
					//this.client.SendMessageAsync(new OnScreenshotResponseMessage() { ScreenShots = new List<Drastic.MauiRemoteImage.Models.Screenshot>() });
				}
				finally
				{
					// Unregister the event handler
					page.Appearing -= handler;
				}
			}
		}
		
		App.Current!.CloseWindow(window);
	}
}

