﻿Imports System.ComponentModel
Imports System.IO
Public Class Form1
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
    Public Shared Property DPI As Single = Form1.CreateGraphics.DpiX / 96

    Public 是否初始化 As Boolean = False
    Private 上一次窗口状态 As FormWindowState

    Public 处理器相关性 As String = ""
    Public 系统状态设定 As Integer = 0
    Public 使用提示音 As Boolean = True
    Public FFmpeg自定义工作目录 As String = ""

    Public 常规流程参数页面 As New 界面_常规流程参数
    Public 混流页面 As New 界面_混流
    Public 合并页面 As New 界面_合并
    Public 编码队列右键菜单 As 暗黑上下文菜单

    Public 性能统计对象 As New 性能统计

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        界面控制.初始化()
        视频编码器数据库.初始化()
        上一次窗口状态 = Me.WindowState
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        界面控制.界面校准()

        If My.Computer.FileSystem.FileExists(Path.Combine(Application.StartupPath, "FontName.txt")) Then
            UiComboBox1.Text = My.Computer.FileSystem.ReadAllText(Path.Combine(Application.StartupPath, "FontName.txt"))
            If UiComboBox1.Text = "" Then Exit Sub
            SetControlFont(UiComboBox1.Text, Me, {UiComboBox1})
        End If

        If DPI <> 1 Then DPI变动时校准界面()

        界面线程执行(AddressOf 插件管理.启动时加载插件)

        If UI同步上下文 Is Nothing Then
            MsgBox("警告：UI 同步上下文是空的，继续使用软件将导致崩溃，请联系开发者排查问题", MsgBoxStyle.Critical)
        End If

        界面线程执行(AddressOf 检查更新.检查)

    End Sub

    Private Sub Form1_DpiChanged(sender As Object, e As DpiChangedEventArgs) Handles Me.DpiChanged
        DPI = e.DeviceDpiNew / 96
        DPI变动时校准界面()
    End Sub

    Public Sub 重新创建句柄()
        Try
            If Not Me.IsHandleCreated Then Me.CreateHandle()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If 是否初始化 = False Then Exit Sub
        If Me.WindowState = FormWindowState.Minimized Then Exit Sub
        If Me.WindowState <> 上一次窗口状态 Then
            界面控制.界面校准()
            上一次窗口状态 = Me.WindowState
        Else
            界面控制.界面校准()
        End If
    End Sub

    Private Sub Form1_Move(sender As Object, e As EventArgs) Handles Me.Move
        If 是否初始化 = False Then Exit Sub
        界面控制.界面校准()
    End Sub

    Sub DPI变动时校准界面()
        Me.MinimumSize = New Size(0, 0)
        Me.Size = New Size(1300 * DPI, 700 * DPI)
        Me.UiTabControlMenu1.ItemSize = New Size(150 * DPI, 40 * DPI)
        Me.ImageList1.ImageSize = New Size(1, 30 * DPI)
        常规流程参数页面.UiTabControl1.ItemSize = New Size(120 * Form1.DPI, 50 * Form1.DPI)
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If 编码任务.队列.Any(Function(task) task.状态 = 编码任务.编码状态.正在处理) Then
            Dim result = MsgBox("有任务正在处理，是否强制关闭所有进程？", MsgBoxStyle.Exclamation + MsgBoxStyle.YesNo)
            If result = MsgBoxResult.No Then
                e.Cancel = True
                Exit Sub
            End If
        End If
        For Each task In 编码任务.队列
            task.清除占用()
        Next
        恢复系统状态()

        If My.Computer.FileSystem.FileExists(Path.Combine(Application.StartupPath, "FontName.txt")) Then
            My.Computer.FileSystem.WriteAllText(Path.Combine(Application.StartupPath, "FontName.txt"), Label11.Font.Name, False)
        End If
        e.Cancel = False

    End Sub

    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub
    Private Sub ListView1_DragDrop(sender As Object, e As DragEventArgs) Handles ListView1.DragDrop
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            界面控制_添加文件.加入编码队列(e.Data.GetData(DataFormats.FileDrop))
        End If
    End Sub
    Private Sub ListView1_KeyDown(sender As Object, e As KeyEventArgs) Handles ListView1.KeyDown
        Select Case e.KeyCode
            Case Keys.A : If e.Control Then 界面控制_编码队列.全选任务()
            Case Keys.Delete : 界面控制_编码队列.移除任务()
        End Select
    End Sub
    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged
        If Me.ListView1.SelectedItems.Count = 1 Then
            Timer1.Enabled = True
            Panel41.Visible = True
            刷新调试信息()
        Else
            Timer1.Enabled = False
            Panel41.Visible = False
        End If
    End Sub

    Sub 刷新调试信息()
        Try
            Label74.Text = 编码任务.队列(Me.ListView1.SelectedItems(0).Index).实时输出
            Label76.Text = String.Join(vbCrLf, 编码任务.队列(Me.ListView1.SelectedItems(0).Index).错误列表)
            If Label76.Text = "" Then
                Panel47.Visible = False
                Label120.Visible = False
            Else
                Panel47.Visible = True
                Label120.Visible = True
                Dim s1 = 根据标签宽度计算显示高度(Label76)
                Label76.Height = s1
                If s1 > TabPage编码队列.Height * 0.3 Then
                    Panel47.Height = TabPage编码队列.Height * 0.3
                Else
                    Panel47.Height = s1
                End If
            End If
        Catch ex As Exception
            编码任务.队列(Me.ListView1.SelectedItems(0).Index).错误列表.Add($"刷新界面失败 {Now}")
        End Try
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        刷新调试信息()
    End Sub



    Private Sub UiButton打开文件显示参数_Click(sender As Object, e As EventArgs) Handles UiButton打开文件显示参数.Click
        Dim openFileDialog As New OpenFileDialog With {.Multiselect = False, .Filter = "所有文件|*.*"}
        If openFileDialog.ShowDialog = DialogResult.OK Then
            显示媒体信息流程(openFileDialog.FileName)
        End If
    End Sub
    Sub 显示媒体信息流程(文件路径 As String)
        Me.RichTextBox1.Text = ""
        Dim FFprobeProcess As New Process
        FFprobeProcess = New Process()
        FFprobeProcess.StartInfo.FileName = "ffprobe"
        FFprobeProcess.StartInfo.WorkingDirectory = If(FFmpeg自定义工作目录 <> "", FFmpeg自定义工作目录, "")
        FFprobeProcess.StartInfo.Arguments = $"-hide_banner ""{文件路径}"""
        FFprobeProcess.StartInfo.RedirectStandardOutput = True
        FFprobeProcess.StartInfo.RedirectStandardError = True
        FFprobeProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8
        FFprobeProcess.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8
        FFprobeProcess.StartInfo.CreateNoWindow = True
        FFprobeProcess.EnableRaisingEvents = True
        AddHandler FFprobeProcess.OutputDataReceived, AddressOf 显示媒体信息输出事件
        AddHandler FFprobeProcess.ErrorDataReceived, AddressOf 显示媒体信息输出事件
        FFprobeProcess.Start()
        FFprobeProcess.BeginOutputReadLine()
        FFprobeProcess.BeginErrorReadLine()
    End Sub
    Sub 显示媒体信息输出事件(sender As Object, e As DataReceivedEventArgs)
        If e.Data Is Nothing Then Exit Sub
        Try
            界面线程执行(Sub() Me.RichTextBox1.AppendText(e.Data & vbCrLf))
        Catch ex As Exception
        End Try
    End Sub
    Private Sub UiButton打开文件显示参数_DragDrop(sender As Object, e As DragEventArgs) Handles UiButton打开文件显示参数.DragDrop
        Dim files() As String = e.Data.GetData(DataFormats.FileDrop)
        If files.Length > 0 Then
            显示媒体信息流程(files(0))
        End If
    End Sub
    Private Sub UiButton打开文件显示参数_DragEnter(sender As Object, e As DragEventArgs) Handles UiButton打开文件显示参数.DragEnter
        If e.Data.GetData(DataFormats.FileDrop) IsNot Nothing Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub
    Private Sub UiTextBox快捷输入CPU核心_KeyPress(sender As Object, e As KeyPressEventArgs)
        Select Case e.KeyChar
            Case "0"c To "9"c, "~"c, ChrW(Keys.Back)
            Case ChrW(Keys.Enter)
                Dim input = UiTextBox快捷输入CPU核心.Text.Trim
                Dim result As New List(Of Integer)
                Try
                    If input.Contains("~"c) Then
                        Dim parts = input.Split("~"c)
                        If parts.Length = 2 Then
                            Dim startNum, endNum As Integer
                            If Integer.TryParse(parts(0), startNum) AndAlso Integer.TryParse(parts(1), endNum) Then
                                If startNum <= endNum Then
                                    For i = startNum To endNum
                                        result.Add(i)
                                    Next
                                    UiTextBox处理器核心.Text = String.Join(",", result)
                                End If
                            End If
                        End If
                    End If
                Catch ex As Exception
                    MsgBox("输入处理异常: " & ex.Message, MsgBoxStyle.Critical)
                End Try
                e.Handled = True
            Case Else
                Exit Sub
        End Select
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        性能统计对象.Update()
        If Panel18.Visible Then
            Dim cpus = 性能统计对象.处理器信息.Keys.ToList
            For i = 0 To cpus.Count - 1
                If i >= Me.ListView3.Items.Count Then
                    Me.ListView3.Items.Add(New ListViewItem)
                    Me.ListView3.Items(i).SubItems.Add("")
                End If
                Me.ListView3.Items(i).SubItems(0).Text = cpus(i)
                Me.ListView3.Items(i).SubItems(1).Text = 性能统计对象.处理器信息(cpus(i))
            Next
        End If

        Dim gpus = 性能统计对象.显卡信息.Keys.ToList
        gpus.Sort()
        For i = 0 To gpus.Count - 1
            If i >= Me.ListView4.Items.Count Then
                Me.ListView4.Items.Add(New ListViewItem)
                Me.ListView4.Items(i).SubItems.Add("")
            End If
            Me.ListView4.Items(i).SubItems(0).Text = gpus(i)
            Me.ListView4.Items(i).SubItems(1).Text = 性能统计对象.显卡信息(gpus(i))
        Next
        While Me.ListView4.Items.Count > gpus.Count
            Me.ListView4.Items.RemoveAt(Me.ListView4.Items.Count - 1)
        End While
    End Sub

    Private Sub UiButton切换处理器占用面板_Click(sender As Object, e As EventArgs) Handles UiButton切换处理器占用面板.Click
        If Panel18.Visible Then
            Panel18.Visible = False
            Panel19.Visible = False
        Else
            Panel18.Visible = True
            Panel19.Visible = True
        End If
        界面控制.界面校准()
    End Sub

    Private Sub UiComboBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles UiComboBox3.SelectedIndexChanged
        For Each C As Control In Panel24.Controls
            Panel24.Controls.Remove(C)
        Next
        If Me.UiComboBox3.Text = "" Then Exit Sub
        If Me.UiComboBox3.SelectedIndex < 0 Then Exit Sub
        Panel24.Controls.Add(插件管理.由插件加载的自定义界面(Me.UiComboBox3.Text))
        插件管理.由插件加载的自定义界面(Me.UiComboBox3.Text).Dock = DockStyle.Fill
    End Sub
End Class
