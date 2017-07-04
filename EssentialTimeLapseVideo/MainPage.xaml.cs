using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

using Windows.Media.Editing;
using Windows.Media.Core;
using Windows.Media.Playback;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.ApplicationModel.Resources;
using Windows.Services.Store;
using Windows.Devices.Enumeration;
using System.Diagnostics;
using Windows.Storage.Search;
using System.Threading;

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
		private MediaEncodingProfile _encodingProfile;  //These are setting attributes, going to do more with these in the future.
		LowLagMediaRecording _mediaRecording;  //This is from one of the posts, it stems off diagnostics, something that will need to be beefed up.
		StorageFolder captureFolder;  //This defaults to the videos folder, that it has perms for.  OpenPicker, allows the user to save anywhere.
		bool isRecording = false;  // recording flag, not fully utilized yet.
		int incognitoer = 0;
		private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");  //I have no idea what this is, but you need it :-)

		int HowManySecondsBetween = 0;
		private bool filejustcreated = false;

		public ResourceLoader languageLoader;
		private StoreContext context = null;
		private string thanks;
		private string f;
		StorageFolder ProjectFolder;
		StorageFolder PictureLapsesFolder ;
		


		public MainPage()
        {
            this.InitializeComponent();
			composition = new MediaComposition();
			CheckForVideoFolders();
			InitCamera();
			InitTime();

			Window.Current.SizeChanged += Current_SizeChanged;

		}

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


			PictureLapsesFolder =  await ProjectFolder.CreateFolderAsync("PictureLapsesFolder");

			if (PictureLapsesFolder == null) return;  //flag some sort of error.

			ProjectName.Text = ProjectFolder.Name;

			return;


		}
		private void InitTime()
		{
			//ComboBoxItem j = new ComboBoxItem();
			//j.Content = "Hours";
			DarnSeconds z = new DarnSeconds();
			//z.HowManyDarnSeconds = 60 * 60;
			//j.Tag = z;
			//HourMinuteSecond.Items.Add(j);

			
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


		}

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
				//await _mediaCapture.StartPreviewAsync();

				// PopulateSettingsComboBox();

			}
			catch (Exception e)
			{

				BadSetting.Visibility = Visibility.Visible;
			}
		}


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


		private async void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
		{

			string errIsCommon = "noError";
			BadSetting.Visibility = Visibility.Collapsed;

			try
			{
				if ((!isRecording) && (CameraSettings2.SelectedIndex > -1))
				{

					/*       if (!skipper)
						   {
							   skipper = !skipper;
							   throw new Exception("Test Exception");
						   }
						   skipper = !skipper;
						   */
					errIsCommon = "Reading Combo Settings";
					ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
					var encodingProperties = (selectedItem.Tag as StreamPropertiesHelper).EncodingProperties;

					/*

                    string grabResolution = selectedItem.Content.ToString();
                    Single width = Convert.ToSingle(grabResolution.Substring(0, grabResolution.IndexOf('x')));





                    Single height = Convert.ToSingle(grabResolution.Substring(grabResolution.IndexOf('x') + 1, grabResolution.IndexOf('[') - (grabResolution.IndexOf('x') + 2)));


                    Single multiplyer = height / width;
                    //Versatile.Width = Window.Current.Bounds.Width - 100;
                    //Versatile.Height = (Window.Current.Bounds.Width - 100) * multiplyer;
                    //Versatile.Width = width / 2;
                    // Versatile.Height = height / 2;


                */
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
			/*var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;

			Resolutions encoderize = selectedItem.Tag as Resolutions;


			Versatile.Height = encoderize.Height;
			Versatile.Width = encoderize.Width;*/



			// await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);

		}

		private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{


			Versatile.Width = e.Size.Width;
			Versatile.Height = e.Size.Height;
			// GetTheVideo.Width = e.Size.Width;
			//GetTheVideo.Height = e.Size.Height;


		}

		

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

		/*private async void startRecording_Tapped(object sender, TappedRoutedEventArgs e)
		{
			try
			{
				isRecording = true;
				//startRecording.Visibility = Visibility.Collapsed;




				//VideoName.IsEnabled = false;
				//GetFileName.IsEnabled = false;
				CameraSettings2.IsEnabled = false;
				CameraSource.IsEnabled = false;








				_encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

				// Create storage file for the capture

				string vidname = "EssentialVideo.mp4";



				if (videoFile == null)
				{

					videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);

				}
				else if (videoFile.Name.Contains("EssentialVideo"))
				{
					videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);

				}
				else if (videoFile.IsAvailable) { }
				else
				{
					Debug.WriteLine("What should I do with this?" + videoFile.Path);
				}
				/*   {
                       if (videoFile.IsAvailable) { } else await vidoeFile.

                       vidname = videoFile.Name;

                   }
                   if (!filejustcreated)
                   {
                       videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);
                   }
                   else filejustcreated = false;
          


				Debug.WriteLine("Starting recording to " + videoFile.Path);

				await _mediaCapture.StartRecordToStorageFileAsync(_encodingProfile, videoFile);
				isRecording = true;

				Debug.WriteLine("Started recording to: " + videoFile.Path);
				stopRecording.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				// File I/O errors are reported as exceptions
				isRecording = false;
				Debug.WriteLine(ex.Message);

			}
		}*/

		private  void stopCapture_Tapped(object sender, TappedRoutedEventArgs e)
		{
			isRecording = false;
			PleaseWait.Visibility = Visibility.Visible;
			
			

		/*	try
			{
			stopCapture.Visibility = Visibility.Collapsed;
				render.Visibility = Visibility.Visible;
				startCapture.Visibility = Visibility.Visible;

				//await _mediaCapture.StopRecordAsync();
				//VideoName.IsEnabled = true;
				//GetFileName.IsEnabled = true;
				CameraSettings2.IsEnabled = true;
				CameraSource.IsEnabled = true;

				IncrementProject.IsEnabled = true;
				Interval.IsEnabled = true;
				HourMinuteSecond.IsEnabled = true;
				
				
				//startRecording.Visibility = Visibility.Visible;
				//VideoName.Text = "Pick New File Name";
				videoFile = null;

			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}*/
		}

		private async void GetFileName_Tapped(object sender, TappedRoutedEventArgs e)
		{

			FileSavePicker fileSavePicker = new FileSavePicker();
			fileSavePicker.FileTypeChoices.Add("MP4 video", new List<string>() { ".MP4" });
			fileSavePicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
			StorageFile file = await fileSavePicker.PickSaveFileAsync();
			if (file != null)
			{
				videoFile = file;
				//VideoName.Text = videoFile.Name;
				filejustcreated = true;

			}
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
				//  int q = 1000;
				//  do { q = 1000; do { q--; } while (q > 0); } while (CameraSource.Items.Count == 0); 

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

				//added to camera and settings initial setting
				//CameraSource.SelectedIndex = 0;
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

				/*if (result.ExtendedError != null)
                {
                    // The user may be offline or there might be some other server failure.
                    storeResult.Text = $"ExtendedError: {result.ExtendedError.Message}";
                    storeResult.Visibility = Visibility.Visible;
                    return;
                }*/

				switch (result.Status)
				{
					case StorePurchaseStatus.AlreadyPurchased:
						storeResult.Text = "The user has already purchased the product.";
						storeResult.Visibility = Visibility.Visible;
						break;

					case StorePurchaseStatus.Succeeded:
						//storeResult.Text = "The purchase was successful.";
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


		private async void TakePicture()
		{
			StorageFile z = await PictureLapsesFolder.CreateFileAsync("Lapses.bmp", CreationCollisionOption.GenerateUniqueName);
			ImageEncodingProperties q = ImageEncodingProperties.CreateBmp();
			//q.Height = 400;
			//q.Width = 400;


			await _mediaCapture.CapturePhotoToStorageFileAsync(q, z);

		}


		/*private async void Reset_Tapped(object sender, TappedRoutedEventArgs e)
		{


			StorageFile z = await  KnownFolders.VideosLibrary.CreateFileAsync("Radio1.bmp", CreationCollisionOption.GenerateUniqueName);

			ImageEncodingProperties q = ImageEncodingProperties.CreateBmp();
			//q.Height = 400;
			//q.Width = 400;


			await _mediaCapture.CapturePhotoToStorageFileAsync(q, z);


		}*/

		private async void FrameCapture()
		{

			List<string> fileTypeFilter = new List<string>();
			fileTypeFilter.Add("*");

			QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);

			//queryOptions = CommonFileQuery.OrderByName;

			queryOptions.UserSearchFilter = "Lapses";
			//StorageFileQueryResult queryResult = musicFolder.CreateFileQueryWithOptions(queryOptions);


			//use the user's input to make a query
			//`````````````````//queryOptions.UserSearchFilter = InputTextBox.Text;
			//StorageFileQueryResult queryResult = musicFolder.CreateFileQueryWithOptions(queryOptions);


			//Windows.Storage.Search.CommonFileQuery.GetName("*.bmp");


			//qbert.

			StorageFileQueryResult z = PictureLapsesFolder.CreateFileQueryWithOptions(queryOptions);

			IReadOnlyList<StorageFile> files = await z.GetFilesAsync();

			if (files.Count == 0)
			{
				NoFiles.Visibility = Visibility.Visible;
				return;
			}


			files.OrderBy(x => x.DateCreated);
			/*
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
			picker.FileTypeFilter.Add(".png");
			Windows.Storage.StorageFile pickedFile = await picker.PickSingleFileAsync();
			if (pickedFile == null)
			{
				//ShowErrorMessage("File picking cancelled");
				return;
			}


	*/


			string desiredName = "test.mp4";
			StorageFolder localFolder = ProjectFolder;
			Windows.Storage.StorageFile pickedVidFile = await localFolder.CreateFileAsync(desiredName, CreationCollisionOption.GenerateUniqueName);

			

			foreach (StorageFile j in files)
			{
				//pickerVid.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
				//pickerVid.FileTypeFilter.Add(".mp4");
				//ndows.Storage.StorageFile pickedVidFile = await localFolder.CreateFileAsync(desiredName, CreationCollisionOption.GenerateUniqueName);
				if (j== null)
				{
					//ShowErrorMessage("File picking cancelled");
					return;
				}

				// These files could be picked from a location that we won't have access to later
				var storageItemAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
				//torageItemAccessList.Add(pickedFile);

				var clip = await MediaClip.CreateFromImageFileAsync(j, TimeSpan.FromSeconds(1));

				composition.Clips.Add(clip);

			}

				//MediaStreamSource MediaStreamSample = composition.GeneratePreviewMediaStreamSource(400, 400);

			//AnythingWorking.SetMediaStreamSource(MediaStreamSample);  This was a media source type in the xaml.


			await composition.RenderToFileAsync(pickedVidFile);

			//await composition.SaveAsync(MediaStreamSample);

		}

		private void Interval_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void HourMinuteSecond_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			/*Interval.Items.Clear();


			

			DarnSeconds f =(DarnSeconds) ((ComboBoxItem)HourMinuteSecond.SelectedItem).Tag;
			int looper = 25;
			if ((f.HowManyDarnSeconds == 1) || (f.HowManyDarnSeconds == 60)) looper = 61;
			
				

			Interval.SelectedIndex = 0;*/
		}

		private void GetProjectName_Tapped(object sender, TappedRoutedEventArgs e)
		{

		}

		private async void startCapture_Tapped(object sender, TappedRoutedEventArgs e)
		{
			startCapture.Visibility = Visibility.Collapsed;
			NoFiles.Visibility = Visibility.Collapsed;
			render.Visibility = Visibility.Collapsed;

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
			//j.HowManyDarnSeconds;

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

		//	if (_mediaCapture == null)
		//	{
		//		await _mediaCapture.InitializeAsync();
		//	}

			while (isRecording)
			{
				
				//if (i++ > 300) isRecording = false;
				StorageFile z = await PictureLapsesFolder.CreateFileAsync("Lapses.bmp", CreationCollisionOption.GenerateUniqueName);
				ImageEncodingProperties q = ImageEncodingProperties.CreateBmp();
				//q.Height = 400;
				//q.Width = 400;


				await _mediaCapture.CapturePhotoToStorageFileAsync(q, z);
				await Task.Delay(HowManySecondsBetween * 1000);
			}


			
			render.Visibility = Visibility.Visible;

			IncrementProject.IsEnabled = false;
			IncrementProject.Opacity = .65;

			//Interval.IsEnabled = false;
			Interval.Visibility = Visibility.Visible;
			tbInterval.Visibility = Visibility.Collapsed;
			//tbInterval.Text = (string)((ComboBoxItem)Interval.SelectedItem).Content;
			HourMinuteSecond.Visibility = Visibility.Visible;
			//tbHourMinuteSecond.Text = (string)((ComboBoxItem)HourMinuteSecond.SelectedItem).Content;
			tbHourMinuteSecond.Visibility = Visibility.Collapsed;
			CameraSource.Visibility = Visibility.Visible;
			tbCameraSource.Visibility = Visibility.Collapsed;
			//tbCameraSource.Text = (string)((ComboBoxItem)CameraSource.SelectedItem).Content;

			CameraSettings2.Visibility = Visibility.Visible;
			tbCameraSetting.Visibility = Visibility.Collapsed;
			//tbCameraSetting.Text = (string)((ComboBoxItem)CameraSettings2.SelectedItem).Content;
			stopCapture.Visibility = Visibility.Collapsed;
			startCapture.Visibility = Visibility.Visible;
			PleaseWait.Visibility = Visibility.Collapsed;


		}


		private void Reset_Tapped(object sender, TappedRoutedEventArgs e)
		{

			// Versatile.Height = 480;
			//Versatile.Width = 640;
			NoFiles.Visibility = Visibility.Collapsed;
			BadDevice.Visibility = Visibility.Collapsed;
			BadSetting.Visibility = Visibility.Collapsed;
			NoCamera.Visibility = Visibility.Collapsed;
			RecordLimit.Visibility = Visibility.Collapsed;
			ManyThanks.Visibility = Visibility.Collapsed;
			CameraSource.Items.Clear();
			InitCamera();
			//CameraSettings2.SelectedIndex = -1;

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
			//Help j;
		
			Frame.Navigate(typeof(Help));
			
			//j.Visibility = Visibility.Visible;
	
		}
	}
	class DarnSeconds
	{
		public int HowManyDarnSeconds;
	}
}
