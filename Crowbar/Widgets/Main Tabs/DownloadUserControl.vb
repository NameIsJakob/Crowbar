Imports System.Collections.Specialized
Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Threading
Imports System.Web
Imports Crowbar.SteamRemoteStorage_PublishedFileDetails_Json

Public Class DownloadUserControl

#Region "Creation and Destruction"

	Public Sub New()
		MyBase.New()
		' This call is required by the designer.
		InitializeComponent()
	End Sub

#End Region

#Region "Init and Free"

	Protected Overrides Sub Init()
		TheApp.InitAppInfo()

		Me.ItemIdTextBox.DataBindings.Add("Text", TheApp.Settings, "DownloadItemIdOrLink", False, DataSourceUpdateMode.OnValidation)

		Me.InitOutputPathComboBox()
		Me.DocumentsOutputPathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
		Me.OutputPathTextBox.DataBindings.Add("Text", TheApp.Settings, "DownloadOutputWorkPath", False, DataSourceUpdateMode.OnValidation)
		Me.UpdateOutputPathWidgets()

		Me.InitDownloadOptions()
		Me.UpdateExampleOutputFileNameTextBox()

		Me.theBackgroundSteamPipe = New BackgroundSteamPipe()

		AddHandler Me.OutputPathTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePathFileName

		AddHandler TheApp.Settings.PropertyChanged, AddressOf AppSettings_PropertyChanged
	End Sub

	' Needed for closing any active child processes. Only called on program exit.
	Protected Overrides Sub Free()
		'Me.CancelDownload()

		If Me.theBackgroundSteamPipe IsNot Nothing Then
			Me.theBackgroundSteamPipe.Kill()
		End If

		'RemoveHandler Me.OutputPathTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePathFileName

		'RemoveHandler TheApp.Settings.PropertyChanged, AddressOf AppSettings_PropertyChanged

		'Me.FreeDownloadOptions()

		'Me.FreeOutputPathComboBox()

		'Me.ItemIdTextBox.DataBindings.Clear()
	End Sub

	Private Sub InitOutputPathComboBox()
		Dim anEnumList As IList

		anEnumList = EnumHelper.ToList(GetType(DownloadOutputPathOptions))
		Try
			Me.OutputPathComboBox.DisplayMember = "Value"
			Me.OutputPathComboBox.ValueMember = "Key"
			Me.OutputPathComboBox.DataSource = anEnumList
			Me.OutputPathComboBox.DataBindings.Add("SelectedValue", TheApp.Settings, "DownloadOutputFolderOption", False, DataSourceUpdateMode.OnPropertyChanged)
		Catch ex As Exception
			Dim debug As Integer = 4242
		End Try
	End Sub

	Private Sub FreeOutputPathComboBox()
		Me.OutputPathComboBox.DataBindings.Clear()
	End Sub

	Private Sub InitDownloadOptions()
		Me.UseIdCheckBox.DataBindings.Add("Checked", TheApp.Settings, "DownloadUseItemIdIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
		Me.PrependTitleCheckBox.DataBindings.Add("Checked", TheApp.Settings, "DownloadPrependItemTitleIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
		Me.AppendDateTimeCheckBox.DataBindings.Add("Checked", TheApp.Settings, "DownloadAppendItemUpdateDateTimeIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
		Me.ReplaceSpacesWithUnderscoresCheckBox.DataBindings.Add("Checked", TheApp.Settings, "DownloadReplaceSpacesWithUnderscoresIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
		Me.ConvertToExpectedFileOrFolderCheckBox.DataBindings.Add("Checked", TheApp.Settings, "DownloadConvertToExpectedFileOrFolderCheckBoxIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
	End Sub

	Private Sub FreeDownloadOptions()
		Me.UseIdCheckBox.DataBindings.Clear()
		Me.PrependTitleCheckBox.DataBindings.Clear()
		Me.AppendDateTimeCheckBox.DataBindings.Clear()
		Me.ReplaceSpacesWithUnderscoresCheckBox.DataBindings.Clear()
		Me.ConvertToExpectedFileOrFolderCheckBox.DataBindings.Clear()
	End Sub

#End Region

#Region "Widget Event Handlers"

	Private Sub DownloadUserControl_Resize(sender As Object, e As EventArgs) Handles Me.Resize
		'NOTE: This code prevents Visual Studio or Windows often inexplicably extending the right side of these widgets.
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.ItemIdTextBox, Me.OpenWorkshopPageButton)
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.OutputPathTextBox, Me.BrowseForOutputPathButton)
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.DocumentsOutputPathTextBox, Me.BrowseForOutputPathButton)
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.DownloadProgressBar, Me.DownloadProgressBar.Parent, True)
	End Sub

#End Region

#Region "Child Widget Event Handlers"

	Private Sub OpenWorkshopPageButton_Click(sender As Object, e As EventArgs) Handles OpenWorkshopPageButton.Click
		Me.OpenWorkshopPage()
	End Sub

	Private Sub OutputPathTextBox_DragDrop(sender As Object, e As DragEventArgs) Handles OutputPathTextBox.DragDrop
		Dim pathFileNames() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
		Dim pathFileName As String = pathFileNames(0)
		If Directory.Exists(pathFileName) Then
			TheApp.Settings.DownloadOutputWorkPath = pathFileName
		End If
	End Sub

	Private Sub OutputPathTextBox_DragEnter(sender As Object, e As DragEventArgs) Handles OutputPathTextBox.DragEnter
		If e.Data.GetDataPresent(DataFormats.FileDrop) Then
			e.Effect = DragDropEffects.Copy
		End If
	End Sub

	Private Sub OutputPathTextBox_Validated(sender As Object, e As EventArgs) Handles OutputPathTextBox.Validated
		Me.UpdateOutputPathTextBox()
	End Sub

	Private Sub BrowseForOutputPathButton_Click(sender As Object, e As EventArgs) Handles BrowseForOutputPathButton.Click
		Me.BrowseForOutputPath()
	End Sub

	Private Sub GotoOutputPathButton_Click(sender As Object, e As EventArgs) Handles GotoOutputPathButton.Click
		Me.GotoOutputPath()
	End Sub

	Private Sub OptionsUseDefaultsButton_Click(sender As Object, e As EventArgs) Handles OptionsUseDefaultsButton.Click
		TheApp.Settings.SetDefaultDownloadOptions()
	End Sub

	Private Sub DownloadButton_Click(sender As Object, e As EventArgs) Handles DownloadButton.Click
		Me.DownloadFromLink()
	End Sub

	Private Sub CancelDownloadButton_Click(sender As Object, e As EventArgs) Handles CancelDownloadButton.Click
		Me.CancelDownload()
	End Sub

	Private Sub UseInUnpackButton_Click(sender As Object, e As EventArgs) Handles UseInUnpackButton.Click
		Me.UseInUnpack()
	End Sub

	Private Sub GotoDownloadedItemButton_Click(sender As Object, e As EventArgs) Handles GotoDownloadedItemButton.Click
		Me.GotoDownloadedItem()
	End Sub

	Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
		Me.LogTextBox.AppendText(".")
	End Sub

#End Region

#Region "Core Event Handlers"

	Private Sub AppSettings_PropertyChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs)
		If e.PropertyName = "DownloadOutputFolderOption" Then
			Me.UpdateOutputPathWidgets()
		ElseIf e.PropertyName = "DownloadUseItemIdIsChecked" Then
			Me.UpdateExampleOutputFileNameTextBox()
		ElseIf e.PropertyName = "DownloadPrependItemTitleIsChecked" Then
			Me.UpdateExampleOutputFileNameTextBox()
		ElseIf e.PropertyName = "DownloadAppendItemUpdateDateTimeIsChecked" Then
			Me.UpdateExampleOutputFileNameTextBox()
		ElseIf e.PropertyName = "DownloadReplaceSpacesWithUnderscoresIsChecked" Then
			Me.UpdateExampleOutputFileNameTextBox()
		End If
	End Sub

	Private Sub DownloadItem_ProgressChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs)
		If e.ProgressPercentage = 0 Then
			Me.LogTextBox.AppendText(CStr(e.UserState))
		ElseIf e.ProgressPercentage = 1 Then
			Dim outputInfo As BackgroundSteamPipe.DownloadItemOutputInfo = CType(e.UserState, BackgroundSteamPipe.DownloadItemOutputInfo)
			Me.theDownloadBytesReceived += outputInfo.BytesReceived
			'Dim progressPercentage As Integer
			''If Me.theDownloadBytesReceived < outputInfo.TotalBytesToReceive Then
			'progressPercentage = CInt(Me.theDownloadBytesReceived * Me.DownloadProgressBar.Maximum / outputInfo.TotalBytesToReceive)
			''Else
			''	progressPercentage = 100
			''End If
			'Me.DownloadProgressBar.Text = Me.theDownloadBytesReceived.ToString() + " / " + outputInfo.TotalBytesToReceive.ToString() + "   " + progressPercentage.ToString() + " %"
			'Me.DownloadProgressBar.Value = progressPercentage
			Me.UpdateProgressBar(Me.theDownloadBytesReceived, outputInfo.TotalBytesToReceive)
		End If
	End Sub

	Private Sub DownloadItem_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
		Dim outputPathFileName As String = Nothing
		Dim targetOutputPath As String = Nothing
		Dim outputInfo As BackgroundSteamPipe.DownloadItemOutputInfo = Nothing

		If e.Cancelled Then
			Me.LogTextBox.AppendText("Download cancelled." + vbCrLf)
			Me.DownloadProgressBar.Text = ""
			Me.DownloadProgressBar.Value = 0
		Else
			outputInfo = CType(e.Result, BackgroundSteamPipe.DownloadItemOutputInfo)
			If outputInfo.Result = "success" Then
				' Me.theDownloadBytesReceived does not have the full byte count and outputInfo.TotalBytesToReceive = 0.
				'Me.UpdateProgressBar(Me.theDownloadBytesReceived, outputInfo.TotalBytesToReceive)
				Me.UpdateProgressBar(outputInfo.ContentFile.Length, outputInfo.ContentFile.Length)

				Dim outputPath As String
				outputPath = Me.GetOutputPath()

				Dim outputFileName As String
				outputFileName = Me.GetOutputFileName(outputInfo.ItemTitle, outputInfo.PublishedItemID, outputInfo.ContentFolderOrFileName, outputInfo.ItemUpdated_Text)

				outputPathFileName = Path.Combine(outputPath, outputFileName)
				outputPathFileName = FileManager.GetTestedPathFileName(outputPathFileName)

				File.WriteAllBytes(outputPathFileName, outputInfo.ContentFile)
				If File.Exists(outputPathFileName) Then
					Me.LogTextBox.AppendText("Download complete." + vbCrLf + "Downloaded file: """ + outputPathFileName + """" + vbCrLf)
					Me.DownloadedItemTextBox.Text = outputPathFileName
					'Me.ProcessFolderOrFileAfterDownload(outputPathFileName)
				Else
					Me.LogTextBox.AppendText("Download failed." + vbCrLf)
				End If
			ElseIf outputInfo.Result = "success_SteamUGC" Then
				Dim outputPath As String
				outputPath = Me.GetOutputPath()

				Dim outputFolder As String
				outputFolder = Me.GetOutputFileName(outputInfo.ItemTitle, outputInfo.PublishedItemID, outputInfo.ContentFolderOrFileName, outputInfo.ItemUpdated_Text)

				targetOutputPath = Path.Combine(outputPath, outputFolder)
				targetOutputPath = FileManager.GetTestedPath(targetOutputPath)

				If Directory.Exists(outputInfo.ContentFolderOrFileName) Then
					'FileManager.CopyFolder(outputInfo.ContentFolderOrFileName, targetOutputPath, True)
					'' [DownloadItem_RunWorkerCompleted] Delete Steam's cached item after downloading SteamUGC item.
					''NOTE: Deleting the folder makes the item un-downloadable for later attempts because Steam still thinks it is installed.
					''      This only occurred because Crowbar used different Steamworks functions calls to download when EItemState.k_EItemStateInstalled was set. 
					''TODO: [DownloadItem_RunWorkerCompleted] Delete Steam's cached item manifest file and cached acf info after downloading SteamUGC item.
					'Directory.Delete(outputInfo.ContentFolderOrFileName, True)
					''======
					''NOTE: UnsubscribeItem() does not delete the folder.
					''Me.UnsubscribeItem(outputInfo.AppID, outputInfo.PublishedItemID)
					'======
					'NOTE: File remains: "C:\Program Files (x86)\Steam\depotcache\<app_id>_<manifest_id>.manifest"
					'NOTE: Data for the downloaded file remains in: "<steam_folder_on_drive_where_game_is_installed>\steamapps\workshop\appworkshop_<app_id>.acf"
					'NOTE: Do not use Directory.Move() because it raises exception when trying to move between drives.
					'Directory.Move(outputInfo.ContentFolderOrFileName, targetOutputPath)
					'======
					My.Computer.FileSystem.MoveDirectory(outputInfo.ContentFolderOrFileName, targetOutputPath)

					If Directory.Exists(targetOutputPath) Then
						'Me.ProcessFolderOrFileAfterDownload(targetOutputPath)
						Me.LogTextBox.AppendText("Download complete." + vbCrLf + "Downloaded folder: """ + targetOutputPath + """" + vbCrLf)
						Me.DownloadedItemTextBox.Text = targetOutputPath
					Else
						Me.LogTextBox.AppendText("Download failed." + vbCrLf)
					End If
				Else
					Me.LogTextBox.AppendText("Download failed." + vbCrLf)
				End If
			End If
		End If

		'Me.DownloadButton.Enabled = True
		'Me.CancelDownloadButton.Enabled = False

		If Not e.Cancelled AndAlso outputInfo IsNot Nothing Then
			If outputInfo.Result = "success" Then
				If File.Exists(outputPathFileName) Then
					Me.ProcessFolderOrFileAfterDownload(outputPathFileName)
				End If
			ElseIf outputInfo.Result = "success_SteamUGC" Then
				If Directory.Exists(targetOutputPath) Then
					Try
						If TheApp.SteamAppInfos.Count > 0 Then
							'NOTE: Use this temp var because appID as a ByRef var can not be used in a lambda expression used in next line.
							Dim steamAppID As New Steamworks.AppId_t(outputInfo.AppID)
							Me.theSteamAppInfo = TheApp.SteamAppInfos.First(Function(info) info.ID = steamAppID)
							Me.ProcessFolderOrFileAfterDownload(targetOutputPath)
						End If
					Catch ex As Exception
						Dim debug As Integer = 4242
					End Try
				End If
			End If
		End If

		Me.DownloadButton.Enabled = True
		Me.CancelDownloadButton.Enabled = False
	End Sub

	Private Sub UnsubscribeItem_ProgressChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs)
		If e.ProgressPercentage = 0 Then
			Me.LogTextBox.AppendText(CStr(e.UserState))
		End If
	End Sub

	Private Sub UnsubscribeItem_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
		'If e.Cancelled Then
		'	Me.LogTextBox.AppendText("Download cancelled." + vbCrLf)
		'Else
		'	Dim outputInfo As BackgroundSteamPipe.DownloadItemOutputInfo = CType(e.Result, BackgroundSteamPipe.DownloadItemOutputInfo)
		'	If outputInfo.Result = "success" Then
		'		Me.LogTextBox.AppendText("Download complete." + vbCrLf)
		'	End If
		'End If

		Dim placeholder As Integer = 4242
	End Sub

#End Region

#Region "Private Methods"

	Private Sub OpenWorkshopPage()
		Dim itemIdOrLink As String = Me.ItemIdTextBox.Text
		Dim itemlink As String = ""
		If itemIdOrLink.StartsWith(AppConstants.WorkshopLinkStart) Then
			itemlink = itemIdOrLink
		Else
			itemlink = AppConstants.WorkshopLinkStart + itemIdOrLink
		End If
		Try
			System.Diagnostics.Process.Start(itemlink)
		Catch ex As Exception
			Dim debug As Integer = 4242
		End Try
	End Sub

	Private Sub UpdateOutputPathTextBox()
		If TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder Then
			If String.IsNullOrEmpty(Me.OutputPathTextBox.Text) Then
				Try
					TheApp.Settings.DownloadOutputWorkPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				Catch ex As Exception
					Dim debug As Integer = 4242
				End Try
			End If
		End If
	End Sub

	Private Sub UpdateOutputPathWidgets()
		Me.DocumentsOutputPathTextBox.Visible = (TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.DocumentsFolder)
		Me.OutputPathTextBox.Visible = (TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder)
		Me.BrowseForOutputPathButton.Enabled = (TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder)
		'Me.GotoOutputPathButton.Enabled = (TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder)
	End Sub

	Private Sub BrowseForOutputPath()
		If TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder Then
			'NOTE: Using "open file dialog" instead of "open folder dialog" because the "open folder dialog" 
			'      does not show the path name bar nor does it scroll to the selected folder in the folder tree view.
			Dim outputPathWdw As New OpenFileDialog()

			outputPathWdw.Title = "Open the folder you want as Output Folder"
			outputPathWdw.InitialDirectory = FileManager.GetLongestExtantPath(TheApp.Settings.DownloadOutputWorkPath)
			If outputPathWdw.InitialDirectory = "" Then
				outputPathWdw.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			End If
			outputPathWdw.FileName = "[Folder Selection]"
			outputPathWdw.AddExtension = False
			outputPathWdw.CheckFileExists = False
			outputPathWdw.Multiselect = False
			outputPathWdw.ValidateNames = False

			If outputPathWdw.ShowDialog() = DialogResult.OK Then
				' Allow dialog window to completely disappear.
				Application.DoEvents()

				TheApp.Settings.DownloadOutputWorkPath = FileManager.GetPath(outputPathWdw.FileName)
			End If
		End If
	End Sub

	Private Sub GotoOutputPath()
		'If TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.DownloadsFolder Then
		'	'TODO: Find way to get the Downloads path. Note that Windows XP does not have a Downloads special folder.
		'	'FileManager.OpenWindowsExplorer(Environment.GetFolderPath(Environment.SpecialFolder.Downloads))
		'Else
		If TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.DocumentsFolder Then
			FileManager.OpenWindowsExplorer(Me.DocumentsOutputPathTextBox.Text)
		ElseIf TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder Then
			FileManager.OpenWindowsExplorer(TheApp.Settings.DownloadOutputWorkPath)
		End If
	End Sub

	Private Sub UseInUnpack()
		Dim extension As String = Path.GetExtension(Me.DownloadedItemTextBox.Text)
		If extension = ".gma" OrElse extension = ".vpk" Then

		End If
		TheApp.Settings.UnpackPackagePathFolderOrFileName = Me.DownloadedItemTextBox.Text
	End Sub

	Private Sub GotoDownloadedItem()
		If Me.DownloadedItemTextBox.Text <> "" Then
			FileManager.OpenWindowsExplorer(Me.DownloadedItemTextBox.Text)
		End If
	End Sub

	Private Async Sub DownloadFromLink()
		Me.LogTextBox.Text = ""
		Me.DownloadProgressBar.Text = ""
		Me.DownloadProgressBar.Value = 0
		Me.theDownloadBytesReceived = 0
		Me.DownloadedItemTextBox.Text = ""
		Me.DownloadButton.Enabled = False
		Me.CancelDownloadButton.Enabled = True

		Dim itemID As String = Me.GetItemID()
		If itemID = "0" Then
			Me.DownloadButton.Enabled = True
			Me.CancelDownloadButton.Enabled = False
			Me.LogTextBox.AppendText("ERROR: Item ID is invalid." + vbCrLf)
			Return
		End If
		Me.theCancellation = New CancellationTokenSource()
		Dim webClient As New HttpClient()
		Me.LogTextBox.AppendText("Getting item content download link..." + vbCrLf)
		Dim downloadDetails As SteamRemoteStorage_PublishedFileDetails_ItemDetail
		Try
			downloadDetails = Await Me.GetPublishedtemDetails(itemID)
		Catch canceled As OperationCanceledException
			Me.DownloadButton.Enabled = True
			Me.CancelDownloadButton.Enabled = False
			Me.LogTextBox.AppendText("Cancelled getting item content download link." + vbCrLf)
			Return
		Catch ex As Exception
			Me.DownloadButton.Enabled = True
			Me.CancelDownloadButton.Enabled = False
			Me.LogTextBox.AppendText("Failed getting item content download link: " + ex.ToString() + vbCrLf)
			Return
		End Try


		Dim appID = downloadDetails.consumer_app_id
		Dim steamAppID As New Steamworks.AppId_t(appID)
		Me.theSteamAppInfo = TheApp.SteamAppInfos.First(Function(info) info.ID = steamAppID)
		If Me.theSteamAppInfo Is Nothing Then
			'NOTE: Value was not found, so unable to download.
			appID = 0
		End If

		If downloadDetails.file_url <> "" Then
			Me.LogTextBox.AppendText("Item content download link found. Downloading file via web." + vbCrLf)
			Me.DownloadViaWeb(downloadDetails)
			Return
		End If

		Me.LogTextBox.AppendText("Item content download link not found. Downloading file via Steam." + vbCrLf)
		Me.DownloadViaSteam(appID, itemID, Me.GetOutputPath())
	End Sub

	Private Sub CancelDownload()
		Me.theCancellation.Cancel()
	End Sub

	Private Function GetItemID() As String
		Dim qscoll As NameValueCollection
		Dim itemID As String = "0"
		Try
			Dim uri As New Uri(Me.ItemIdTextBox.Text)
			Dim querystring As String = uri.Query
			'Dim separators() = {"="}
			'id = querystring.Split()
			qscoll = HttpUtility.ParseQueryString(querystring)
			itemID = qscoll("id")
		Catch ex1 As UriFormatException
			Dim text As String = Me.ItemIdTextBox.Text
			itemID = ""
			Dim pos As Integer = text.IndexOf("id=")
			If pos >= 0 Then
				text = text.Remove(0, pos + 3)
				For Each c As Char In text
					If IsNumeric(c) Then
						itemID += c
					Else
						Exit For
					End If
				Next
			Else
				'NOTE: Get first run of numeric characters.
				Dim foundNumeric As Boolean = False
				For Each c As Char In text
					If IsNumeric(c) Then
						itemID += c
						foundNumeric = True
					ElseIf foundNumeric Then
						Exit For
					End If
				Next
			End If
		Catch ex As Exception
			Dim debug As Integer = 4242
		End Try

		If itemID = "" Then
			itemID = "0"
		End If

		Return itemID
	End Function

	Private Async Function GetPublishedtemDetails(itemID As String) As Task(Of SteamRemoteStorage_PublishedFileDetails_ItemDetail)
		Dim client As New HttpClient()
		Dim requestArguments = {
			KeyValuePair.Create("itemcount", "1"),
			KeyValuePair.Create("publishedfileids[0]", itemID)
		}
		Dim response = Await client.PostAsync("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", New FormUrlEncodedContent(requestArguments), Me.theCancellation.Token)
		response.EnsureSuccessStatusCode()
		Dim content = Await response.Content.ReadFromJsonAsync(Of SteamRemoteStorage_PublishedFileDetails_Json)(Me.theCancellation.Token)
		Return content.response.publishedfiledetails(0)
	End Function

	Private Async Sub DownloadViaWeb(details As SteamRemoteStorage_PublishedFileDetails_ItemDetail)
		Dim uri As Uri = New Uri(details.file_url)

		Dim outputPath As String
		outputPath = Me.GetOutputPath()
		Try
			FileManager.CreatePath(outputPath)
		Catch ex As Exception
			Me.LogTextBox.AppendText("Crowbar tried to create folder path """ + outputPath + """, but Windows gave this message: " + ex.Message + vbCrLf)
			Exit Sub
		End Try

		Dim outputFileName As String
		outputFileName = Me.GetOutputFileName(details.title, details.publishedfileid, details.filename, details.time_updated.ToString())

		Dim outputPathFileName As String
		outputPathFileName = Path.Combine(outputPath, outputFileName)
		outputPathFileName = FileManager.GetTestedPathFileName(outputPathFileName)

		Me.LogTextBox.AppendText("Downloading workshop item as: """ + outputPathFileName + """" + vbCrLf)

		Dim client As New HttpClient()
		Try
			Dim response = Await client.GetAsync(uri, Me.theCancellation.Token)
			response.EnsureSuccessStatusCode()
			Dim responseStream = Await response.Content.ReadAsStreamAsync(Me.theCancellation.Token)
			Dim byteReadCount = 0
			Using outputFile = File.OpenWrite(outputPathFileName)
				Dim buffer(UShort.MaxValue - 1) As Byte
				Do
					Dim bytesRead = Await responseStream.ReadAsync(buffer, 0, buffer.Length, Me.theCancellation.Token)
					byteReadCount += bytesRead
					Me.UpdateProgressBar(byteReadCount, response.Content.Headers.ContentLength.Value)
					If bytesRead = 0 Then Exit Do
					Await outputFile.WriteAsync(buffer, 0, bytesRead, Me.theCancellation.Token)
				Loop
			End Using
		Catch canceled As OperationCanceledException
			Me.LogTextBox.AppendText("Download cancelled." + vbCrLf)
			Me.DownloadProgressBar.Text = ""
			Me.DownloadProgressBar.Value = 0
			Me.DownloadButton.Enabled = True
			Me.CancelDownloadButton.Enabled = False
			If File.Exists(outputPathFileName) Then
				Try
					File.Delete(outputPathFileName)
				Catch ex As Exception
					Me.LogTextBox.AppendText("WARNING: Problem deleting incomplete downloaded file." + vbCrLf)
				End Try
			End If
			Return
		Catch e As Exception
			Me.LogTextBox.AppendText("Download failed: " + e.ToString() + vbCrLf)
			Me.DownloadProgressBar.Text = ""
			Me.DownloadProgressBar.Value = 0
			Me.DownloadButton.Enabled = True
			Me.CancelDownloadButton.Enabled = False
			If File.Exists(outputPathFileName) Then
				Try
					File.Delete(outputPathFileName)
				Catch ex As Exception
					Me.LogTextBox.AppendText("WARNING: Problem deleting incomplete downloaded file." + vbCrLf)
				End Try
			End If
			Return
		End Try

		Me.LogTextBox.AppendText("Download complete." + vbCrLf + "Downloaded file: """ + outputPathFileName + """" + vbCrLf)
		Me.DownloadedItemTextBox.Text = outputPathFileName

		Me.ProcessFolderOrFileAfterDownload(outputPathFileName)

		Me.DownloadButton.Enabled = True
		Me.CancelDownloadButton.Enabled = False
	End Sub

	Private Sub DownloadViaSteam(ByVal appID As UInteger, ByVal itemID As String, ByVal targetPath As String)
		'Me.theDownloadBytesReceived = 0
		'Me.DownloadedItemTextBox.Text = ""
		'Me.DownloadButton.Enabled = False
		'Me.CancelDownloadButton.Enabled = True

		Dim inputInfo As New BackgroundSteamPipe.DownloadItemInputInfo()
		inputInfo.AppID = appID
		inputInfo.PublishedItemID = itemID
		inputInfo.TargetPath = targetPath
		Me.theBackgroundSteamPipe.DownloadItem(AddressOf Me.DownloadItem_ProgressChanged, AddressOf Me.DownloadItem_RunWorkerCompleted, inputInfo)
	End Sub

	Private Sub UnsubscribeItem(ByVal appID As UInteger, ByVal itemID As String)
		Dim inputInfo As New BackgroundSteamPipe.DownloadItemInputInfo()
		inputInfo.AppID = appID
		inputInfo.PublishedItemID = itemID
		Me.theBackgroundSteamPipe.UnsubscribeItem(AddressOf Me.UnsubscribeItem_ProgressChanged, AddressOf Me.UnsubscribeItem_RunWorkerCompleted, inputInfo)
	End Sub

	Private Function GetOutputPath() As String
		Dim outputPath As String = ""

		If TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.DocumentsFolder Then
			outputPath = Me.DocumentsOutputPathTextBox.Text
		ElseIf TheApp.Settings.DownloadOutputFolderOption = DownloadOutputPathOptions.WorkFolder Then
			outputPath = TheApp.Settings.DownloadOutputWorkPath
		End If

		'This will change a relative path to an absolute path.
		outputPath = Path.GetFullPath(outputPath)
		Return outputPath
	End Function

	Private Sub UpdateExampleOutputFileNameTextBox()
		Me.ExampleOutputFileNameTextBox.Text = Me.GetOutputFileName("Example Title With Spaces", "00000000", "ExampleFileName.vpk", "0")
	End Sub

	Private Function GetOutputFileName(ByVal givenTitle As String, ByVal givenID As String, ByVal givenFileName As String, ByVal givenTimeUpdatedText As String) As String
		Dim outputFileNamePrefix As String
		If TheApp.Settings.DownloadPrependItemTitleIsChecked Then
			outputFileNamePrefix = givenTitle + "_"
		Else
			outputFileNamePrefix = ""
		End If

		Dim outputFileNameBase As String
		If TheApp.Settings.DownloadUseItemIdIsChecked Then
			outputFileNameBase = givenID
		Else
			outputFileNameBase = Path.GetFileNameWithoutExtension(givenFileName)
		End If

		Dim outputFileNameSuffix As String
		If TheApp.Settings.DownloadAppendItemUpdateDateTimeIsChecked Then
			Dim fileDateTime As DateTime
			fileDateTime = MathModule.UnixTimeStampToDateTime(Long.Parse(givenTimeUpdatedText))
			outputFileNameSuffix = "_" + fileDateTime.ToString("yyyy-MM-dd-HHmm")
		Else
			outputFileNameSuffix = ""
		End If

		Dim fileExtension As String = ""
		fileExtension = Path.GetExtension(givenFileName)

		Dim outputFileName As String
		outputFileName = outputFileNamePrefix + outputFileNameBase + outputFileNameSuffix + fileExtension
		If TheApp.Settings.DownloadReplaceSpacesWithUnderscoresIsChecked Then
			outputFileName = outputFileName.Replace(" ", "_")
		End If

		'NOTE: Remove colons here to prevent GetCleanPathFileName() from removing everything up to first colon.
		outputFileName = outputFileName.Replace(":", "_")
		outputFileName = FileManager.GetCleanPathFileName(outputFileName, False)
		outputFileName = outputFileName.Replace("\", "_")

		Return outputFileName
	End Function

	Private Sub UpdateProgressBar(ByVal bytesReceived As Long, ByVal totalBytesToReceive As Long)
		Try
			Dim progressPercentage As Integer = CInt(bytesReceived * Me.DownloadProgressBar.Maximum / totalBytesToReceive)
			Me.DownloadProgressBar.Text = bytesReceived.ToString("N0") + " / " + totalBytesToReceive.ToString("N0") + " bytes   " + progressPercentage.ToString() + " %"
			Me.DownloadProgressBar.Value = progressPercentage
		Catch ex As Exception
			Dim debug As Integer = 4242
		End Try
	End Sub

	Private Sub ProcessFolderOrFileAfterDownload(ByRef pathFileName As String)
		If Me.theSteamAppInfo IsNot Nothing AndAlso TheApp.Settings.DownloadConvertToExpectedFileOrFolderCheckBoxIsChecked Then
			Try
				'Me.DownloadButton.Enabled = False
				'Me.CancelDownloadButton.Enabled = True

				Me.theProcessAfterDownloadWorker = New BackgroundWorkerEx()
				Me.theProcessAfterDownloadWorker.WorkerSupportsCancellation = True
				Me.theProcessAfterDownloadWorker.WorkerReportsProgress = True
				AddHandler Me.theProcessAfterDownloadWorker.DoWork, AddressOf ProcessAfterDownloadWorker_DoWork
				AddHandler Me.theProcessAfterDownloadWorker.ProgressChanged, AddressOf ProcessAfterDownloadWorker_ProgressChanged
				AddHandler Me.theProcessAfterDownloadWorker.RunWorkerCompleted, AddressOf ProcessAfterDownloadWorker_RunWorkerCompleted
				Me.theProcessAfterDownloadWorker.RunWorkerAsync(pathFileName)
			Catch ex As Exception
				Me.LogTextBox.AppendText("ERROR: " + ex.Message + vbCrLf)
			End Try
		End If
	End Sub

	'NOTE: This is run in a background thread.
	Private Sub ProcessAfterDownloadWorker_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
		Dim givenPathFileName As String = CType(e.Argument, String)
		Dim convertedPathFileName As String = Me.theSteamAppInfo.ProcessFileAfterDownload(givenPathFileName, Me.theProcessAfterDownloadWorker)
		If convertedPathFileName = givenPathFileName Then
			e.Result = ""
		Else
			e.Result = convertedPathFileName
		End If
	End Sub

	Private Sub ProcessAfterDownloadWorker_ProgressChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs)
		If e.ProgressPercentage = 0 Then
			Me.LogTextBox.AppendText(CStr(e.UserState))
			'ElseIf e.ProgressPercentage = 1 Then
			'	Me.LogTextBox.AppendText(vbTab + CStr(e.UserState))
		End If
	End Sub

	Private Sub ProcessAfterDownloadWorker_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
		If e.Cancelled Then
		Else
			Dim pathFileName As String = CType(e.Result, String)
			If pathFileName <> "" Then
				Me.LogTextBox.AppendText("Converted to file: """ + pathFileName + """" + vbCrLf)
				'Me.DownloadedItemTextBox.Text = pathFileName
			End If
		End If

		RemoveHandler Me.theProcessAfterDownloadWorker.DoWork, AddressOf ProcessAfterDownloadWorker_DoWork
		RemoveHandler Me.theProcessAfterDownloadWorker.ProgressChanged, AddressOf ProcessAfterDownloadWorker_ProgressChanged
		RemoveHandler Me.theProcessAfterDownloadWorker.RunWorkerCompleted, AddressOf ProcessAfterDownloadWorker_RunWorkerCompleted
		Me.theProcessAfterDownloadWorker = Nothing

		'Me.DownloadButton.Enabled = True
		'Me.CancelDownloadButton.Enabled = False
	End Sub

#End Region

#Region "Data"

	Private theCancellation As CancellationTokenSource
	Private theProcessAfterDownloadWorker As BackgroundWorkerEx
	Private theAppIdText As String
	Private theSteamAppInfo As SteamAppInfoBase

	Private theBackgroundSteamPipe As BackgroundSteamPipe

	Private theDownloadBytesReceived As Long

#End Region

End Class
