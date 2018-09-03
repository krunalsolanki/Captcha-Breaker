Imports System.IO
Imports System.Drawing

Public Class MainWindow

    Dim MatchThreshold As Integer = 0
    Dim Filename As String = ""
    Dim Preview As Bitmap
    Dim NoiseTolerance As Integer = 120
    Dim xbounds As New List(Of List(Of Integer))
    Dim fbounds As New List(Of List(Of Integer))
    Dim clist As New List(Of Bitmap)
    Dim chars As New List(Of String)
    Dim report As New List(Of String)



    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub btnOpenFile_Click(sender As Object, e As EventArgs) Handles btnOpenFile.Click
        OpenFileCaptcha.ShowDialog()
    End Sub

    Private Sub OpenFileCaptcha_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileCaptcha.FileOk
        Filename = OpenFileCaptcha.FileName
        txtFile.Text = Filename
        txtFile.Update()
        Preview = New Bitmap(Filename)
        picPreview.Image = Preview
        picPreview.Update()
    End Sub

    ''' <summary>
    ''' Being the actual Sovle
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub btnSolve_Click(sender As Object, e As EventArgs) Handles btnSolve.Click
        If txtFile.Text = "" Then
            MsgBox("You need to select a file!", MsgBoxStyle.Critical)
        Else
            btnSolve.Enabled = False
            btnOpenFile.Enabled = False
            txtCharacters.Text = ""
            txtCharacters.Update()
            chars.Clear()
            clist.Clear()
            fbounds.Clear()
            xbounds.Clear()
            report.Clear()

            AddReportItem("Solve Started")

            'run the denoising/bounding alogrithm
            pbProgress.Value = 0
            Solve()
            AddReportItem("Denoising and Deobfuscating completed")


            If MatchThreshold = 0 Then
                MsgBox("Test Noise Removal")
            Else
                pbProgress.Value = 0
                pbProgress.Maximum = clist.Count
                pbProgress.Update()
                Dim x As Integer = 0
                For Each b As Bitmap In clist
                    Threading.Thread.Sleep(100)
                    AddReportItem("Matching Character:" & x)
                    Match(b)
                    pbProgress.Value = x + 1
                    pbProgress.Update()
                    x += 1
                Next

                AddReportItem("Outputting Characters")
                For Each s As String In chars
                    txtCharacters.Text += s
                Next
                AddReportItem("Solve Finished")
                AddReportItem("Characters Found: " & txtCharacters.Text)
                Dim r As New Dialog1
                r.ReportItems = report
                r.ShowDialog()
            End If

            pbProgress.Value = 0
            pbProgress.Update()
            btnSolve.Enabled = True
            btnOpenFile.Enabled = True
            resetPreview()
        End If


    End Sub


#Region "Image Handling"

    Private Sub Solve()
        pbProgress.Maximum = 5

        RemoveBorder()
        pbProgress.Value = 1
        pbProgress.Update()
        RemoveNoise()
        pbProgress.Value = 2
        pbProgress.Update()
        AddBoundingBoxes()
        pbProgress.Value = 3
        pbProgress.Update()
        CreateCharacterBitmapList(True)
        pbProgress.Value = 4
        pbProgress.Update()
    End Sub

    Private Sub resetPreview()
        Preview = New Bitmap(Filename)
        picPreview.Image = Preview
        picPreview.Update()
    End Sub

    Private Sub UpdatePreview()
        picPreview.Image = Preview
        picPreview.Update()
    End Sub

    Private Sub RemoveBorder()
        For x As Integer = 0 To Preview.Width - 1
            For y As Integer = 0 To Preview.Height - 1
                If (x = 0) Or (x = Preview.Width - 1) Then
                    Preview.SetPixel(x, y, Color.White)
                End If
                If (y = 0) Or (y = Preview.Height - 1) Then
                    Preview.SetPixel(x, y, Color.White)
                End If
            Next
            UpdatePreview()
        Next
        AddReportItem("External Border removed.")
    End Sub

    ''' <summary>
    ''' takes an average of RGB and if less than tolerance, removes pixel.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub RemoveNoise()
        For x As Integer = 0 To Preview.Width - 1
            For y As Integer = 0 To Preview.Height - 1
                Dim average As Integer = 0
                average = (CInt(Preview.GetPixel(x, y).R) + CInt(Preview.GetPixel(x, y).G) + CInt(Preview.GetPixel(x, y).B)) / 3
                If average > NoiseTolerance Then
                    Preview.SetPixel(x, y, Color.White)
                Else
                    Preview.SetPixel(x, y, Color.Black)
                End If
            Next
            UpdatePreview()
        Next
        AddReportItem("Noise Removal Completed")
    End Sub

    Private Sub AddBoundingBoxes()
        'add longtitudinal lines
        Dim started As Boolean = False
        Dim startingx As Integer = 0
        pbProgress.Value = 0
        pbProgress.Maximum = Preview.Width - 1
        For x As Integer = 0 To Preview.Width - 1
            pbProgress.Value = x
            pbProgress.Update()
            Dim empty As Boolean = True
            For y As Integer = 0 To Preview.Height - 1
                Dim c As Color = Preview.GetPixel(x, y)
                If Not c.Name = "ffffffff" Then
                    empty = False

                End If
            Next
            'if it passes, draw the line!

            If empty = True Then
                If started = True Then
                    Dim node As New List(Of Integer)
                    node.Add(startingx)
                    node.Add(x - 1)
                    xbounds.Add(node)
                    started = False
                End If


                For y As Integer = 0 To Preview.Height - 1
                    Preview.SetPixel(x, y, Color.Gray)
                Next
            Else
                If started = False Then
                    started = True
                    startingx = x
                End If
            End If
            UpdatePreview()
        Next

        'find lateral lines
        pbProgress.Value = 0
        'pbProgress.Maximum = xbounds.Count
        For Each l As List(Of Integer) In xbounds
            pbProgress.Value += 1
            pbProgress.Update()
            'for each xbound          
            Dim tly As Integer = 0
            Dim bry As Integer = 0
            Dim tlx As Integer = 0
            Dim brx As Integer = 0
            Dim ystarted As Boolean = False
            For y As Integer = 0 To Preview.Height - 1
                Dim clear As Boolean = True
                For x As Integer = l.Item(0) To l.Item(1)
                    If Preview.GetPixel(x, y).Name <> "ffffffff" Then
                        clear = False
                    End If
                Next

                If clear = True Then
                    If ystarted = True Then
                        ystarted = False
                        brx = l.Item(1)
                        bry = y - 1
                    End If
                    For x As Integer = l.Item(0) To l.Item(1)
                        Preview.SetPixel(x, y, Color.Gray)
                    Next
                Else
                    If ystarted = False Then
                        tlx = l.Item(0)
                        tly = y
                        ystarted = True
                    End If
                End If
                UpdatePreview()
            Next
            Dim nodeb As New List(Of Integer)
            'add top left x coord
            nodeb.Add(tlx)
            'add top left y coord
            nodeb.Add(tly)
            'add bottom right x coord
            nodeb.Add(brx)
            'add bottom right y coord
            nodeb.Add(bry)
            'add the final bounds to the bound list
            fbounds.Add(nodeb)
            AddReportItem("Bounding Box creation completed")
        Next

    End Sub


    Private Sub CreateCharacterBitmapList(Optional ByVal saveImage = False)

        For i As Integer = 0 To fbounds.Count - 1
            Try


                'copt bound region to canvas top left
                Dim cb As List(Of Integer) = fbounds.Item(i)
                'get bounds, making sure there are 4 bounds (tlx,tly,brx,bry)
                If cb.Count = 4 Then
                    Dim tlx As Integer = cb.Item(0)
                    Dim tly As Integer = cb.Item(1)
                    Dim w As Integer = cb.Item(2) - tlx
                    Dim h As Integer = cb.Item(3) - tly


                    Dim region As New Rectangle(tlx, tly, w, h)

                    Dim b As Bitmap = bmpCopy(Preview, region)

                    Dim timeStamp As Integer = GenerateTimestamp(Date.Now)
                    If saveImage = True Then
                        b.Save(Environment.CurrentDirectory & "\training\UNMATCHED\" & timeStamp & ".bmp")
                    End If

                    clist.Add(b)
                    Threading.Thread.Sleep(100)

                End If





            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        Next
    End Sub

    Private Function bmpCopy(srcBitmap As Bitmap, _
  section As Rectangle) As Bitmap

        ' Create the new bitmap and associated graphics object
        Dim bmp As New Bitmap(50, 50)
        Dim g As Graphics = Graphics.FromImage(bmp)
        g.Clear(Color.White)
        ' Draw the specified section of the source bitmap to the new one
        g.DrawImage(srcBitmap, 0, 0, section, GraphicsUnit.Pixel)

        ' Clean up
        g.Dispose()

        ' Return the bitmap
        Return bmp

    End Function 'Copy


#End Region

#Region "matching"

    Public Shared Function GetFilesRecursive(ByVal initial As String) As List(Of String)
        ' This list stores the results.
        Dim result As New List(Of String)

        Dim dir As New DirectoryInfo(initial)

        For Each d As DirectoryInfo In dir.GetDirectories
            If (d.Name <> "UNMATCHED") Then
                Dim f() As FileInfo = d.GetFiles()

                For Each File As FileInfo In f
                    result.Add(File.FullName)
                Next
            End If


        Next

        ' Return the list
        Return result
    End Function


    Private Sub Match(ByVal bitmapToMatch As Bitmap)
        Dim TrainingSet As New List(Of String)
        TrainingSet = GetFilesRecursive(Environment.CurrentDirectory & "/training")

        Dim bestMatchPath As String = ""
        Dim bestMatchPercent As Double = 0


        For Each tsi As String In TrainingSet
            Try
                Dim tempb As New Bitmap(tsi)
                Dim matches As Integer = 0
                Dim total As Integer = 0
                If (bitmapToMatch.Width = tempb.Width) And (bitmapToMatch.Height = tempb.Height) Then
                    For x As Integer = 0 To tempb.Width - 1
                        For y As Integer = 0 To tempb.Height - 1
                            If tempb.GetPixel(x, y).Name = "ff000000" Then
                                total += 1
                                If tempb.GetPixel(x, y).Name = bitmapToMatch.GetPixel(x, y).Name Then
                                    matches += 1
                                End If
                            End If
                        Next
                    Next
                End If

                Dim percent As Double = (matches / total) * 100

                If percent > bestMatchPercent Then
                    bestMatchPercent = percent
                    bestMatchPath = tsi
                End If



                tempb.Dispose()
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try

        Next


        If bestMatchPercent >= MatchThreshold Then
            If bestMatchPath <> "" Then
                Dim f As New FileInfo(bestMatchPath)
                If f.Directory.Name <> "IGNORE" Then
                    AddReportItem("Character matched as: " & f.Directory.Name & " (Tested " & TrainingSet.Count & " images) (" & Math.Round(bestMatchPercent, 2) & "% Match)")
                    chars.Add(f.Directory.Name)
                Else
                    AddReportItem("Character Matched, but is IGNORED character. (Tested " & TrainingSet.Count & " images)")
                End If

            Else
                AddReportItem("No matching image found. Outputting training image. (Tested " & TrainingSet.Count & " images)")
                chars.Add("-")
                Dim timeStamp As Integer = GenerateTimestamp(Date.Now)
                bitmapToMatch.Save(Environment.CurrentDirectory & "\training\UNMATCHED\" & timeStamp & ".bmp")
                Threading.Thread.Sleep(1500)
            End If

        Else
            AddReportItem("Matching failed to meet threshold. Outputting training image. (Tested " & TrainingSet.Count & " images) (" & Math.Round(bestMatchPercent, 2) & "% Match)")
            chars.Add("-")
            Dim timeStamp As Integer = GenerateTimestamp(Date.Now)
            bitmapToMatch.Save(Environment.CurrentDirectory & "\training\UNMATCHED\" & timeStamp & ".bmp")
            Threading.Thread.Sleep(1500)
        End If
        'MsgBox(f.Directory.Name)
    End Sub

#End Region


    Public Function GenerateTimestamp(ByVal dteDate As Date) As String
        If dteDate.IsDaylightSavingTime = True Then
            dteDate = DateAdd(DateInterval.Hour, -1, dteDate)
        End If
        GenerateTimestamp = DateDiff(DateInterval.Second, #1/1/1970#, dteDate)
    End Function


    Public Sub AddReportItem(ByVal message As String)
        report.Add(Date.Now & "> " & message)
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Application.Exit()
    End Sub



End Class
