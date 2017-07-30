/*
            Program: Essential Time Lapse Video
            Author:  John Leone
            Email:   gibbloggen@gmail.com
            Date:    7-4-2017
            License: MIT License
            Purpose: A Universal Windows app for win 10 desktop



The MIT License (MIT) 
Copyright (c) <2017> <John Leone, gibbloggen@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


References for this program,,,

1)   https://msdn.microsoft.com/en-us/windows/uwp/audio-video-camera/camera  Took bits and pieces of this very helpful
2)   Various Stack Overflow searches.
3)   This was developed from my earlier work, and also a current program in the windows 10 store.  That program is multi-lingual
   it is my ambition to make this multilingual also in the future, but, for now, it is english only.
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Editing;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.ApplicationModel.Resources;
using Windows.Services.Store;
using Windows.Devices.Enumeration;
using System.Diagnostics;
using Windows.Storage.Search;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EssentialTimeLapseVideo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
		private MediaComposition composition;


		MediaCapture _mediaCapture;  //This is the basic capture that you pass to the xaml
		StorageFile videoFile = null;  // This is the file that it will record to.  Changeable by user.
		StorageFolder captureFolder;  //This defaults to the videos folder, that it has perms for.  OpenPicker, allows the user to save anywhere.
		private MediaEncodingProfile _encodingProfile;  //These are setting attributes, going to do more with these in the future.
		LowLagMediaRecording _mediaRecording;  //This is from one of the posts, it stems off diagnostics, something that will need to be beefed up.
		bool isRecording = false;  // recording flag, not fully utilized yet.
		private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");  //I have no idea what this is, but you need it :-)
		private bool filejustcreated = false;
		int HowManySecondsBetween = 0;
		
		public ResourceLoader languageLoader;
		private StoreContext context = null;
		private string f;
		StorageFolder ProjectFolder;
		StorageFolder PictureLapsesFolder ;
		


		public MainPage()
        {
            this.InitializeComponent();

			
			ApplicationView.GetForCurrentView().Title = "Version " + GetAppVersion() + " ";  
			

			composition = new MediaComposition();
			CheckForVideoFolders();
			InitCamera();
			InitTimeAndFPS();

			Window.Current.SizeChanged += Current_SizeChanged;

		}


		//This was copied form this Stack Overflow Entry
		//https://stackoverflow.com/questions/28635208/retrieve-the-current-app-version-from-package/28635481#28635481
		public static string GetAppVersion()
		{

			Package package = Package.Current;
			PackageId packageId = package.Id;
			PackageVersion version = packageId.Version;

			return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

		}

		//This just checks for the programs folder in the Video Folder
		// if it isn't there, it creates it.  This is the reason it requires the Video folder

		// It also checks this folder for project files.  The first project file in the 
		//sequence that is not there, it will make the folder.
		//even if you proj1 proj2  and then proj4, it will make the next one proj3
		private async void CheckForVideoFolders()
		{

			String projectName = "";

			StorageFolder j = KnownFolders.VideosLibrary;

			IStorageItem k = await j.TryGetItemAsync("EssentialTimeLapseVideo");

			if (k == null)
			{
				k = await j.CreateFolderAsync("EssentialTimeLapseVideo");
			}
			j = (StorageFolder) k;
			if (k == null)
			{
				//tell them file error
				return;
			}

			for (int i = 1; i < 200; i++)
			{
				IStorageItem l = await j.TryGetItemAsync(String.Format("Project {0:D3}", i));
				if (l == null)
				{
					projectName = (String.Format("Project {0:D3}", i));
					i = 201;
					


				}

			}


			ProjectFolder = await j.CreateFolderAsync(projectName);

			if (ProjectFolder == null) return;  //flag some sort of error.


			//this is the subfolder that holds all the picture files.

			PictureLapsesFolder =  await ProjectFolder.CreateFolderAsync("PictureLapsesFolder");

			if (PictureLapsesFolder == null) return;  //flag some sort of error.

			ProjectName.Text = ProjectFolder.Name;

			return;


		}

		//This is to init the two combo boxes 1-60for time, and seconds or minutes 
		private void InitTimeAndFPS()
		{

			//had trouble with this, so made a little class, it did the trick, but could be reworked.
			
			DarnSeconds z = new DarnSeconds();
			
			
			ComboBoxItem k = new ComboBoxItem();
			k.Content = "Minutes";
			DarnSeconds y = new DarnSeconds();
			y.HowManyDarnSeconds = 60;

			k.Tag = y;
			HourMinuteSecond.Items.Add(k);

			ComboBoxItem l = new ComboBoxItem();
			l.Content = "Seconds";
			DarnSeconds w = new DarnSeconds();
			w.HowManyDarnSeconds = 1;
			l.Tag = w;
			HourMinuteSecond.Items.Add(l);

			HourMinuteSecond.SelectedIndex = 1;

			int looper = 61;
			for (int i = 1; i < looper; i++)
			{
				ComboBoxItem j = new ComboBoxItem();

				j.Content = i.ToString();
				DarnSeconds t = new DarnSeconds();
				t.HowManyDarnSeconds = i;

				j.Tag = t;
				Interval.Items.Add(j);

			}
			Interval.SelectedIndex = 0;

			//This is the math for the frame per second of rendering calculation,
			//it generates it in milliseconds.
			decimal MilliForFPS = (decimal)1000;

			looper = 61;
			for (int i = 1; i < looper; i++)
			{
				ComboBoxItem j = new ComboBoxItem();

				j.Content = i.ToString();
				DarnSeconds t = new DarnSeconds();
				Decimal fps = (Decimal)MilliForFPS/i;
				t.HowManyDarnSeconds = (int)Math.Round(fps,0);

				j.Tag = t;
				FramesPerSecond.Items.Add(j);

			}

			FramesPerSecond.SelectedIndex = 9;

		}


		//This is taken right out of Essential Video Recorder, it records streaming video
		//and was the first video app I made.  This one is different in that it takes frames
		//and then renders them together.  EVR recorded straight video.

		private async void InitCamera()

		{
			stopCapture.Visibility = Visibility.Collapsed;
			var videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
			captureFolder = videosLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;


			DeviceInformationCollection j = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);




			var z = j.Count();

			var Oz = j.OrderBy(x => x.Name);

			if (j.Count == 0) //messagebox in all languages, no device.
			{

				NoCamera.Visibility = Visibility.Visible;
				return;
			}

			try
			{
				string[] dups = new string[j.Count];
				int WhichDevice = 0;
				string nameThatCamera = "";
				int howManyCameras = 0;
				foreach (DeviceInformation q in Oz)
				{
					nameThatCamera = q.Name;
					if (WhichDevice > 0)
					{
						for (int v = 0; v < WhichDevice; v++)
						{
							if (nameThatCamera == dups[v]) { howManyCameras++; }
						}
						if (howManyCameras == 0) { }
						else
						{
							howManyCameras++;
							nameThatCamera = nameThatCamera + '-' + howManyCameras.ToString();
						}
						howManyCameras = 0;
					}
					dups[WhichDevice] = q.Name;
					WhichDevice++;
					ComboBoxItem comboBoxItem = new ComboBoxItem();
					comboBoxItem.Content = nameThatCamera;

					comboBoxItem.Tag = q;
					CameraSource.Items.Add(comboBoxItem);

				}
			}
			catch (Exception e)
			{

				BadDevice.Visibility = Visibility.Visible;

			}

			try
			{



				DeviceInformation gotCamera = (DeviceInformation)Oz.First();
				MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
				settings.VideoDeviceId = gotCamera.Id;
				_mediaCapture = new MediaCapture();

				await _mediaCapture.InitializeAsync();
				_mediaCapture.Failed += _mediaCapture_Failed;

				_mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


				GetTheVideo.Source = _mediaCapture;

				CheckIfStreamsAreIdentical();

				MediaStreamType streamType;

				streamType = MediaStreamType.VideoRecord;
				PopulateStreamPropertiesUI(streamType, CameraSettings2);

				//added to camera and settings initial setting
				CameraSource.SelectedIndex = 0;
				CameraSettings2.SelectedIndex = 0;
				
			}
			catch (Exception e)
			{

				BadSetting.Visibility = Visibility.Visible;
			}
		}


		//This is taken right out of Essential Video Recorder, it records streaming video
		//and was the first video app I made.  This one is different in that it takes frames
		//and then renders them together.  EVR recorded straight video.


		private void PopulateStreamPropertiesUI(MediaStreamType streamType, ComboBox comboBox, bool showFrameRate = true)
		{
			// Query all properties of the specified stream type 
			IEnumerable<StreamPropertiesHelper> allStreamProperties =
				_mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamPropertiesHelper(x));

			// Order them by resolution then frame rate
			allStreamProperties = allStreamProperties.Where(x => x.FrameRate > 9).OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

			// Populate the combo box with the entries
			foreach (var property in allStreamProperties)
			{
				ComboBoxItem comboBoxItem = new ComboBoxItem();
				comboBoxItem.Content = property.GetFriendlyName(showFrameRate);
				comboBoxItem.Tag = property;
				comboBox.Items.Add(comboBoxItem);
			}
		}


		//This is taken right out of Essential Video Recorder, it records streaming video
		//and was the first video app I made.  This one is different in that it takes frames
		//and then renders them together.  EVR recorded straight video.
		
			//It originally came from a Microsoft example somewhere.
		private async void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
		{

			string errIsCommon = "noError";
			BadSetting.Visibility = Visibility.Collapsed;

			try
			{
				if ((!isRecording) && (CameraSettings2.SelectedIndex > -1))
				{

					
					errIsCommon = "Reading Combo Settings";
					ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
					var encodingProperties = (selectedItem.Tag as StreamPropertiesHelper).EncodingProperties;

					errIsCommon = "MediaCapture Failure";
					await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);


					
					errIsCommon = "Window Sizing Error";
					Versatile.Width = Window.Current.Bounds.Width;
					Versatile.Height = Window.Current.Bounds.Height;
				}
			}
			catch (Exception ex)
			{
				BadSetting.Visibility = Visibility.Visible;

			}
			
		}


		//This is how the window sizing is wired, so you move the window and the video sizes appropriatley, 
		//some of this is also in the xaml for the page.
		private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{


			Versatile.Width = e.Size.Width;
			Versatile.Height = e.Size.Height;
		

		}


		//This is taken right out of Essential Video Recorder, it records streaming video
		//and was the first video app I made.  This one is different in that it takes frames
		//and then renders them together.  EVR recorded straight video.


		private async void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
		{
			try
			{
				await _mediaRecording.StopAsync();
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("Emergency stop sync failed.");
			}
			isRecording = false;
			System.Diagnostics.Debug.WriteLine("Media Capture Failed");
		}

		//This came from a Tutorial page, with all my camera's, Logitech, they are all identical.  But may need it for other users in the future.
		private void CheckIfStreamsAreIdentical()
		{
			if (_mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.AllStreamsIdentical ||
				_mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.PreviewRecordStreamsIdentical)
			{
				System.Diagnostics.Debug.WriteLine("Preview and video streams for this device are identical. Changing one will affect the other");
			}
		}




		//This is taken right out of Essential Video Recorder, it records streaming video
		//and was the first video app I made.  This one is different in that it takes frames
		//and then renders them together.  EVR recorded straight video.

		private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
		{
			try
			{
				await _mediaRecording.StopAsync();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}


			RecordLimit.Visibility = Visibility.Visible;
			System.Diagnostics.Debug.WriteLine("Record limitation exceeded.");
			isRecording = false;
		}
	
		

		private void Versatile_Tapped(object sender, TappedRoutedEventArgs e)
		{

		}

	
		private  void stopCapture_Tapped(object sender, TappedRoutedEventArgs e)
		{
			isRecording = false;
			PleaseWait.Visibility = Visibility.Visible;

			
		
		}

	


		private async void Devicechanged()
		{
			try
			{

				await _mediaCapture.StopPreviewAsync();

				_mediaCapture.Dispose();

			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);

			}

			try
			{
				ComboBoxItem selectedItem = (ComboBoxItem)CameraSource.SelectedItem;

				DeviceInformation gotCamera = selectedItem.Tag as DeviceInformation;

				MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
				settings.VideoDeviceId = gotCamera.Id;
				System.Diagnostics.Debug.WriteLine("Cam ID" + gotCamera.Id.ToString());
				_mediaCapture = new MediaCapture();

				await _mediaCapture.InitializeAsync(settings);
				_mediaCapture.Failed += _mediaCapture_Failed;

				_mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


				GetTheVideo.Source = _mediaCapture;


				MediaStreamType streamType;

				CameraSettings2.Items.Clear();
				streamType = MediaStreamType.VideoRecord;
				PopulateStreamPropertiesUI(streamType, CameraSettings2);

				CameraSettings2.SelectedIndex = 0;

				await _mediaCapture.StartPreviewAsync();




			}
			catch (Exception x)
			{

			}

		}


		private void Devicechanged_Changed2(object sender, SelectionChangedEventArgs e)
		{
			if (CameraSource.Items.Count == 0) return;

			Devicechanged();
		}

		private void makeDonation_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Donator.Visibility == Visibility.Visible)
				Donator.Visibility = Visibility.Collapsed;
			else Donator.Visibility = Visibility.Visible;

			storeResult.Visibility = Visibility.Collapsed;


		}


		//This came from some sort of Microsoft Tutorial
		public async void PurchaseAddOn(string storeId)
		{


			try
			{
				

				if (context == null)
				{
					context = StoreContext.GetDefault();

				}

				workingProgressRing.IsActive = true;
				StorePurchaseResult result = await context.RequestPurchaseAsync(storeId);
				workingProgressRing.IsActive = false;

				

				switch (result.Status)
				{
					case StorePurchaseStatus.AlreadyPurchased:
						storeResult.Text = "The user has already purchased the product.";
						storeResult.Visibility = Visibility.Visible;
						break;

					case StorePurchaseStatus.Succeeded:
						//storeResult.Text = "The purchase was successful.";
						ManyThanks.Text = "Many Thanks!";
						ManyThanks.Visibility = Visibility.Visible;
						break;

					case StorePurchaseStatus.NotPurchased:
						storeResult.Text = "The user cancelled the purchase.";
						storeResult.Visibility = Visibility.Visible;
						break;

					case StorePurchaseStatus.NetworkError:
						storeResult.Text = "The purchase was unsuccessful due to a network error.";
						storeResult.Visibility = Visibility.Visible;
						break;

					case StorePurchaseStatus.ServerError:
						storeResult.Visibility = Visibility.Visible;
						storeResult.Text = "The purchase was unsuccessful due to a server error.";
						break;

					default:
						storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
						storeResult.Visibility = Visibility.Visible;
						break;
				}
			}
			catch (Exception ex)
			{
				storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
				storeResult.Visibility = Visibility.Visible;
			}
		}

		//These codes are set per app.  They will only run from the given app.

		private void Donation1_Tapped(object sender, TappedRoutedEventArgs e)
		{
			PurchaseAddOn("9ntnk5dvbfj2");

			Donator.Visibility = Visibility.Collapsed;
		}

		private void Donation2_Tapped(object sender, TappedRoutedEventArgs e)
		{
			PurchaseAddOn("9npx3cs7gxwp");
			Donator.Visibility = Visibility.Collapsed;
		}

		private void Donation3_Tapped(object sender, TappedRoutedEventArgs e)
		{
			PurchaseAddOn("9pk4xzqslk5w");
			Donator.Visibility = Visibility.Collapsed;
		}


		

		private async void FrameCapture()
		{

			RenderComplete.Visibility = Visibility.Collapsed;

			composition = new MediaComposition();

			List<string> fileTypeFilter = new List<string>();
			fileTypeFilter.Add("*");

			QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);

		
			queryOptions.UserSearchFilter = "Lapses";
			
			StorageFileQueryResult z = PictureLapsesFolder.CreateFileQueryWithOptions(queryOptions);

			IReadOnlyList<StorageFile> files = await z.GetFilesAsync();

			if (files.Count == 0)
			{
				NoFiles.Visibility = Visibility.Visible;
				return;
			}


			files.OrderBy(x => x.DateCreated);
		

			String framesPS = (FramesPerSecond.SelectedIndex + 1).ToString("D2");

			string desiredName = "Rendered-" + framesPS + "-FPS.mp4";

			StorageFolder localFolder = ProjectFolder;
			Windows.Storage.StorageFile pickedVidFile = await localFolder.CreateFileAsync(desiredName, CreationCollisionOption.GenerateUniqueName);


			DarnSeconds q = (DarnSeconds)((ComboBoxItem)FramesPerSecond.SelectedItem).Tag;

			

			foreach (StorageFile j in files)
			{
				if (j== null)
				{
					//ShowErrorMessage("File picking cancelled");
					return;
				}

				// These files could be picked from a location that we won't have access to later
				var storageItemAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
				//torageItemAccessList.Add(pickedFile);
				

				var clip = await MediaClip.CreateFromImageFileAsync(j, TimeSpan.FromMilliseconds(q.HowManyDarnSeconds));

				composition.Clips.Add(clip);

			}

		


			await composition.RenderToFileAsync(pickedVidFile);

			RenderComplete.Visibility = Visibility.Visible;

			//await composition.SaveAsync(MediaStreamSample);

		}

		private void Interval_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void HourMinuteSecond_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void GetProjectName_Tapped(object sender, TappedRoutedEventArgs e)
		{

		}

		private async void startCapture_Tapped(object sender, TappedRoutedEventArgs e)
		{
			startCapture.Visibility = Visibility.Collapsed;
			NoFiles.Visibility = Visibility.Collapsed;
			render.Visibility = Visibility.Collapsed;
			RenderComplete.Visibility = Visibility.Collapsed;

			IncrementProject.IsEnabled = false;
			IncrementProject.Opacity = .65;

			//Interval.IsEnabled = false;
			Interval.Visibility = Visibility.Collapsed;
			tbInterval.Visibility = Visibility.Visible;
			tbInterval.Text = (string)((ComboBoxItem)Interval.SelectedItem).Content;
			HourMinuteSecond.Visibility = Visibility.Collapsed;
			tbHourMinuteSecond.Text = (string)((ComboBoxItem)HourMinuteSecond.SelectedItem).Content;
			tbHourMinuteSecond.Visibility = Visibility.Visible;
			CameraSource.Visibility = Visibility.Collapsed;
			tbCameraSource.Visibility = Visibility.Visible; 
			tbCameraSource.Text = (string)((ComboBoxItem)CameraSource.SelectedItem).Content;

			CameraSettings2.Visibility = Visibility.Collapsed;
			tbCameraSetting.Visibility = Visibility.Visible; 
			tbCameraSetting.Text = (string)((ComboBoxItem)CameraSettings2.SelectedItem).Content;

			


			






			
			DarnSeconds j = (DarnSeconds)((ComboBoxItem)Interval.SelectedItem).Tag;

			String k = (string)((ComboBoxItem)HourMinuteSecond.SelectedItem).Content;
		

			if (k  == "Hours")
			{
				HowManySecondsBetween = j.HowManyDarnSeconds * 360;

			}
			else if (k == "Minutes")
			{
				HowManySecondsBetween = j.HowManyDarnSeconds * 60;
			}
			else HowManySecondsBetween = j.HowManyDarnSeconds;


			int i = 1;


			isRecording = true;
			stopCapture.Visibility = Visibility.Visible;

		
			while (isRecording)
			{
				
			StorageFile z = await PictureLapsesFolder.CreateFileAsync("Lapses.png", CreationCollisionOption.GenerateUniqueName);
				ImageEncodingProperties q = ImageEncodingProperties.CreatePng();
			

				await _mediaCapture.CapturePhotoToStorageFileAsync(q, z);
				await Task.Delay(HowManySecondsBetween * 1000);
			}


			
			render.Visibility = Visibility.Visible;

			
			IncrementProject.IsEnabled = true;
			IncrementProject.Opacity = 1.0;

			Interval.Visibility = Visibility.Visible;
			tbInterval.Visibility = Visibility.Collapsed;
			HourMinuteSecond.Visibility = Visibility.Visible;
			tbHourMinuteSecond.Visibility = Visibility.Collapsed;
			CameraSource.Visibility = Visibility.Visible;
			tbCameraSource.Visibility = Visibility.Collapsed;
			
			CameraSettings2.Visibility = Visibility.Visible;
			tbCameraSetting.Visibility = Visibility.Collapsed;
			stopCapture.Visibility = Visibility.Collapsed;
			startCapture.Visibility = Visibility.Visible;
			PleaseWait.Visibility = Visibility.Collapsed;


		}


		private void Reset_Tapped(object sender, TappedRoutedEventArgs e)
		{
			isRecording = false;

			startCapture.Visibility = Visibility.Visible;
			render.Visibility = Visibility.Visible;
			RenderComplete.Visibility = Visibility.Collapsed;

			IncrementProject.IsEnabled = true;
			IncrementProject.Opacity = 1.0;

			Interval.Visibility = Visibility.Visible;
			tbInterval.Visibility = Visibility.Collapsed;
			HourMinuteSecond.Visibility = Visibility.Visible;
			tbHourMinuteSecond.Visibility = Visibility.Collapsed;

			CameraSource.Visibility = Visibility.Visible;
			tbCameraSource.Visibility = Visibility.Collapsed;
			CameraSettings2.Visibility = Visibility.Visible;
			tbCameraSetting.Visibility = Visibility.Collapsed;

			FramesPerSecond.Visibility = Visibility.Visible;
			FramesPerSecond.SelectedIndex = 9;
			fps.Visibility = Visibility.Collapsed;

			Donator.Visibility = Visibility.Collapsed;
			storeResult.Visibility = Visibility.Collapsed;
			ManyThanks.Visibility = Visibility.Collapsed;


			NoFiles.Visibility = Visibility.Collapsed;
			BadDevice.Visibility = Visibility.Collapsed;
			BadSetting.Visibility = Visibility.Collapsed;
			NoCamera.Visibility = Visibility.Collapsed;
			RecordLimit.Visibility = Visibility.Collapsed;
			ManyThanks.Visibility = Visibility.Collapsed;
			CameraSource.Items.Clear();
			InitCamera();

			Interval.SelectedIndex = 0;


		}
		private void render_Tapped(object sender, TappedRoutedEventArgs e)
		{
			FrameCapture();
		}


		private void IncrementProject_Tapped(object sender, TappedRoutedEventArgs e)
		{
			CheckForVideoFolders();
		}

		private void CameraSource_Tapped(object sender, TappedRoutedEventArgs e)
		{
			
		}

		private void HelpPage_Tapped(object sender, TappedRoutedEventArgs e)
		{
			
		
			Frame.Navigate(typeof(Help));
			
			
	
		}

		private void FramesPerSecond_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}
	}
	class DarnSeconds
	{
		public int HowManyDarnSeconds;
	}
}
