﻿namespace QQ_piracy.MusicForms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Microsoft.WindowsAPICodePack.Taskbar;
    using QQ_piracy.Model;
    using QQ_piracy.Properties;
    using MenuItem = QQ_piracy.Model.MenuItem;

    public partial class MusicMainForm : Form
    {
        // 打开文件的默认文件位置
        private const string DefaultSongsFilePath = @"C:\Users\Rhine\Music";
        private string localSongsFilePath = Application.StartupPath + "\\songListHistory.txt"; // 本地音乐的记录文件
        private string favoriteSongsFilePath = Application.StartupPath + "\\favoriteSongs.txt"; // 本地音乐的记录文件

        SongsInfo currSelectedSong = new SongsInfo(null);       // 用于查看详情，打开本地歌曲右键菜单
        SongsInfo currPlaySong = new SongsInfo(null);       // 记录当前选中播放的歌曲

        // 用于保存本地歌曲的链表
        private List<SongsInfo> localSongsList = new List<SongsInfo>();
        private List<SongsInfo> favoriteSongsList = new List<SongsInfo>(); // 用于保存收藏歌曲
        private List<SongsInfo> oringinListSong;                // 用于搜索功能

        private ThumbnailToolbarButton ttbbtnPlayPause;  // 用于底部的缩略图的播放按钮 ico
        private ThumbnailToolbarButton ttbbtnPre;
        private ThumbnailToolbarButton ttbbtnNext;

        private int[] randomList;           // 随机播放序列
        private int randomListIndex = 0;    // 序列索引
        private int jumpSongIndex;          // 跳过当前播放的歌曲

        // 随机0，单曲循环1，列表循环2
        public enum PlayMode
        {
            Shuffle = 0,
            SingleLoop,
            ListLoop,
        }

        public PlayMode currPlayMode = PlayMode.Shuffle;

        Point downPoint; // 用于设置拖动设置的位置

        List<MenuItem> menuItemList;    // 界面左边的菜单列表

        public MusicMainForm()
        {
            InitializeComponent();
            // testAWM.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(AxWmp_PlayStateChange);

            MenuItem item1 = new MenuItem(Resources.list, "List");
            MenuItem item2 = new MenuItem(Resources.favorite, "Favorite");
            MenuItem item3 = new MenuItem(Resources.user, "User");
            MenuItem item4 = new MenuItem(Resources.album, "Album");
            MenuItem item5 = new MenuItem(Resources.ranking, "Rank");
            MenuItem item6 = new MenuItem(Resources.star, "Function");
            MenuItem item7 = new MenuItem(Resources.musicLibrary, "Music library");
            MenuItem item8 = new MenuItem(Resources.message, "Message");
            this.menuItemList = new List<MenuItem>();
            menuItemList.Add(item1);
            menuItemList.Add(item2);
            menuItemList.Add(item3);
            menuItemList.Add(item4);
            menuItemList.Add(item5);
            menuItemList.Add(item6);
            menuItemList.Add(item7);
            menuItemList.Add(item8);

            lbMenu.Items.Add("本地音乐");
            lbMenu.Items.Add("收藏音乐");
            lbMenu.Items.Add("Music library");
            lbMenu.Items.Add("User");
            lbMenu.Items.Add("Album");
            lbMenu.Items.Add("Rank");
            lbMenu.Items.Add("Function");
            lbMenu.Items.Add("Message");

            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 30);    // 分别是宽和高
            lvSongList.SmallImageList = imgList;

            MyColorTable myColorTable = new MyColorTable();
            cmsSongListMenu.Renderer = new ToolStripProfessionalRenderer(myColorTable);
            cmsSongListMenu.ForeColor = Color.White;
            cmsSongListMenu.BackColor = Color.FromArgb(48, 47, 51);

            pbAddSong.Visible = false;
        }

        private void MusicMainForm_Load(object sender, EventArgs e)
        {
            // 设置文件打开窗口（添加音乐）可多选
            this.openFileDialog1.Multiselect = true;

            // 重置播放器状态信息
            ReloadStatus();

            // 读取播放器列表历史记录
            localSongsList = ReadHistorySongsList(localSongsFilePath);
            favoriteSongsList = ReadHistorySongsList(favoriteSongsFilePath);

            // 设置专辑图片控件到顶部页面（z-index)
            // pbAlbumImage.BringToFront();

            // 设置开机自启
            // StarUp("0");
        }

        /// <summary>
        /// 主窗体关闭
        /// </summary>
        private void MusicMainForm_Closed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
            this.Dispose();
        }

        private void MusicMainForm_Shown(object sender, EventArgs e)
        {
            // 暂停按钮
            ttbbtnPlayPause = new ThumbnailToolbarButton(Properties.Resources.播放1, "播放");
            ttbbtnPlayPause.Enabled = true;
            ttbbtnPlayPause.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(pbPlay_Click);

            // 上一首按钮
            ttbbtnPre = new ThumbnailToolbarButton(Properties.Resources.上一首1, "上一首");
            ttbbtnPre.Enabled = true;
            ttbbtnPre.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(pbBack_Click);

            // 下一首按钮
            ttbbtnNext = new ThumbnailToolbarButton(Properties.Resources.下一首1, "下一首");
            ttbbtnNext.Enabled = true;
            ttbbtnNext.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(pbNext_Click);
            TaskbarManager.Instance.ThumbnailToolbars.AddButtons(this.Handle, ttbbtnPre, ttbbtnPlayPause, ttbbtnNext);
        }

        /// <summary>
        /// 播放器状态改变触发事件
        /// </summary>
        private void AxWmp_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            switch (e.newState)
            {
                case 0: // Stopped 未知状态
                    break;

                case 1: // Stopped 停止
                    timerPlay.Stop();
                    ReloadStatus();
                    break;

                case 2: // Paused 暂停
                    timerPlay.Stop();
                    break;

                case 3: // Playing 正在播放
                    timerPlay.Start();

                    // 显示专辑图片
                    // pbAlbumImage.Image = currPlaySong.AlbumImage;
                    pbSmallAlbum.BackgroundImage = currPlaySong.SmallAblum;

                    // 显示歌曲标题名字
                    labelMusicDetail.Text = currPlaySong.FileName + "-" + currPlaySong.Artist;
                    if (currPlaySong.FileName.Length > 30)
                    {
                        labelMusicDetail.Text = currPlaySong.FileName.Substring(0, 30) + "...";
                    }
                    else
                    {
                        labelMusicDetail.Text = currPlaySong.FileName;
                    }

                    tackBarMove.Maximum = (int)axWindowsMediaPlayer1.currentMedia.duration;

                    int currIndex = lvSongList.SelectedItems[0].Index;
                    lvSongList.SelectedItems.Clear();
                    lvSongList.Items[currIndex].Selected = true;    // 设定选中
                    lvSongList.Items[currIndex].EnsureVisible();    // 保证可见
                    lvSongList.Items[currIndex].Focused = true;
                    lvSongList.Select();
                    break;

                case 4:    // ScanForward
                    //tsslCurrentPlayState.Text = "ScanForward";
                    break;

                case 5:    // ScanReverse
                    //tsslCurrentPlayState.Text = "ScanReverse";
                    break;
                case 6:    // Buffering
                    //tsslCurrentPlayState.Text = "正在缓冲";
                    break;

                case 7:    // Waiting
                    //tsslCurrentPlayState.Text = "Waiting";
                    break;

                case 8:    // MediaEnded
                    //tsslCurrentPlayState.Text = "MediaEnded";
                    break;

                case 9:    // Transitioning
                    //tsslCurrentPlayState.Text = "正在连接";
                    break;

                case 10:   // Ready
                    //tsslCurrentPlayState.Text = "准备就绪";
                    break;

                case 11:   // Reconnecting
                    //tsslCurrentPlayState.Text = "Reconnecting";
                    break;

                case 12:   // Last
                    //tsslCurrentPlayState.Text = "Last";
                    break;
                default:
                    //tsslCurrentPlayState.Text = ("Unknown State: " + e.newState.ToString());
                    break;
            }

            if (axWindowsMediaPlayer1.playState.ToString() == "wmppsMediaEnded")
            {
                // 获取音乐播放文件路径，并添加到播放控件
                string path = GetPath();
                WMPLib.IWMPMedia media = axWindowsMediaPlayer1.newMedia(path);
                axWindowsMediaPlayer1.currentPlaylist.appendItem(media);
            }
        }

        /// <summary>
        /// 重置播放器的状态
        /// </summary>
        private void ReloadStatus()
        {
            // 设置专辑封面为默认
            pbSmallAlbum.Image = Properties.Resources.defaultSmallAblum;
            labelMusicTimer.Text = "00:00 / 00:00";
            tackBarMove.Value = tackBarMove.Maximum / 2;
            tackBarMove.Value = 0;
            if (lvSongList.Items.Count > 0 && lvSongList.SelectedItems.Count == 0)
            {
                lvSongList.Items[0].Selected = true; // 设定选中
                lvSongList.Items[0].EnsureVisible(); // 保证可见
                lvSongList.Items[0].Focused = true;
            }
        }

        /// <summary>
        /// 设置播放模式
        /// </summary>
        private string GetPath()
        {
            int currIndex = lvSongList.SelectedItems[0].Index;
            switch (currPlayMode)
            {
                case PlayMode.ListLoop:
                    if (currIndex != lvSongList.Items.Count - 1)
                    {
                        currIndex += 1;
                    }
                    else
                    {
                        currIndex = 0;
                    }

                    break;
                case PlayMode.SingleLoop:
                    break;
                case PlayMode.Shuffle:
                    // 当前循环结束
                    if (randomListIndex > randomList.Length - 1)
                    {
                        StarNewRound();
                    }

                    // 匹配到需要跳过的歌曲，当前循环结束
                    if (randomList[randomListIndex] == jumpSongIndex)
                    {
                        if (randomListIndex == randomList.Length - 1)
                        {
                            StarNewRound();
                        }
                        else
                        {
                            randomListIndex++;
                        }
                    }

                    currIndex = randomList[randomListIndex++];

                    break;
            }

            lvSongList.Items[currIndex].Selected = true; // 设定选中
            lvSongList.Items[currIndex].EnsureVisible(); // 保证可见
            lvSongList.Items[currIndex].Focused = true;
            currPlaySong = new SongsInfo(lvSongList.SelectedItems[0].SubItems[6].Text);

            return currPlaySong.FilePath;
        }

        private void StarNewRound()
        {
            // 重新生成随机序列
            BuildRandomList(lvSongList.Items.Count);

            // 第二轮开始 播放所有歌曲 不跳过
            jumpSongIndex = -1;
        }

        /// <summary>
        /// 用于创建随机序列，随机播放
        /// </summary>
        private void BuildRandomList(int songListCount)
        {
            randomListIndex = 0;
            randomList = new int[songListCount];

            // 初始化序列
            for (int i = 0; i < songListCount; i++)
            {
                randomList[i] = i;
            }

            // 随机序列
            for (int i = songListCount - 1; i >= 0; i--)
            {
                Random r = new Random(Guid.NewGuid().GetHashCode());
                int j = r.Next(0, songListCount - 1);
                swap(randomList, i, j);
            }
        }

        private void swap(int[] arr, int a, int b)
        {
            int temp = arr[a];
            arr[a] = arr[b];
            arr[b] = temp;
        }

        /// <summary>
        /// 设置开机自启
        /// </summary>
        private void StarUp(string flag)
        {
            string path = Application.StartupPath;
            string keyName = path.Substring(path.LastIndexOf("//") + 1);
            Microsoft.Win32.RegistryKey rkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (flag.Equals("1"))
            {
                if (rkey == null)
                {
                    rkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                }

                rkey.SetValue(keyName, path + @"\SimpleMediaPlayer.exe");
            }
            else
            {
                if (rkey != null)
                {
                    rkey.DeleteValue(keyName, false);
                }
            }
        }

        /// <summary>
        /// 鼠标进入上一首图标时
        /// </summary>
        private void pbBack_MouseHover(object sender, EventArgs e)
        {
            pbBack.Image = Resources.上一首hover;
        }

        /// <summary>
        /// 鼠标离开上一首图标时
        /// </summary>
        private void pbBack_MouseLeave(object sender, EventArgs e)
        {
            pbBack.Image = Resources.上一首;
        }

        /// <summary>
        /// 鼠标进入播放图标时
        /// </summary>
        private void pbPlay_MouseHover(object sender, EventArgs e)
        {
            if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                pbPlay.Image = Resources.暂停hover;
            }
            else
            {
                pbPlay.Image = Resources.播放hover;
            }
        }

        /// <summary>
        /// 鼠标离开播放图标时
        /// </summary>
        private void pbPlay_MouseLeave(object sender, EventArgs e)
        {
            if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                pbPlay.Image = Resources.暂停;
            }
            else
            {
                pbPlay.Image = Resources.播放;
            }
        }

        /// <summary>
        /// 鼠标进入下一首图标时
        /// </summary>
        private void pbNext_MouseHover(object sender, EventArgs e)
        {
            pbNext.Image = Resources.下一首hover;
        }

        /// <summary>
        /// 鼠标离开下一首图标
        /// </summary>
        private void pbNext_MouseLeave(object sender, EventArgs e)
        {
            pbNext.Image = Resources.下一首;
        }

        /// <summary>
        /// 鼠标进入声音图标
        /// </summary>
        private void pbVolume_MouseHover(object sender, EventArgs e)
        {
            panelMusicVlume.Visible = true;
        }

        /// <summary>
        /// 鼠标出声音图标
        /// </summary>
        private void pbVolume_MouseLeave(object sender, EventArgs e)
        {
            Point p1 = new Point(this.pbVolume.Location.X, this.pbVolume.Location.Y + 523);
            Point p2 = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y);

            // 判断鼠标是否在panelMusicVlume上，是就不执行，否则就是该panel隐藏
            // bool b = this.RectangleToScreen(panelMusicVlume.ClientRectangle).Contains(MousePosition);
            if ((p1.X - p2.X > 0) || (p1.X - p2.X < -24) || p2.Y > 572)
            {
                panelMusicVlume.Visible = false;
            }
        }

        /// <summary>
        /// 鼠标离开panel
        /// </summary>
        private void panelMusicVlume_MouseLeave(object sender, EventArgs e)
        {
            Point p1 = new Point(this.panelMusicVlume.Location.X, this.panelMusicVlume.Location.Y);
            Point p2 = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y);

            // 判断鼠标是否还在该控件上，不在再隐藏
            if ((p1.X - p2.X > 0) || (p1.X - p2.X < -24) || p2.Y < 405)
            {
                panelMusicVlume.Visible = false;
            }
        }

        /// <summary>
        /// 鼠标停留
        /// </summary>
        private void pbMusicStyle_MouseHover(object sender, EventArgs e)
        {
            pbMusicStyle.Image = Resources.音效hover;
        }

        /// <summary>
        /// 鼠标离开
        /// </summary>
        private void pbMusicStyle_MouseLeave(object sender, EventArgs e)
        {
            pbMusicStyle.Image = Resources.音效;
        }

        /// <summary>
        /// Hifi鼠标停留
        /// </summary>
        private void pbHiFi_MouseHover(object sender, EventArgs e)
        {
            pbHiFi.Image = Resources.HiFihover;
        }

        /// <summary>
        /// Hifi鼠标离开
        /// </summary>
        private void pbHiFi_MouseLeave(object sender, EventArgs e)
        {
            pbHiFi.Image = Resources.HiFi;
        }

        /// <summary>
        /// 歌词按钮鼠标停留
        /// </summary>
        private void pbLyric_MouseHover(object sender, EventArgs e)
        {
            pbLyric.Image = Resources.词hover;
        }

        /// <summary>
        /// 歌词按钮鼠标移出
        /// </summary>
        private void pbLyric_MouseLeave(object sender, EventArgs e)
        {
            pbLyric.Image = Resources.词;
        }

        /// <summary>
        /// 收藏按钮鼠标停留
        /// </summary>
        private void pbLike_MouseHover(object sender, EventArgs e)
        {
            pbLike.Image = Resources.收藏hover;
        }

        /// <summary>
        /// 收藏按钮鼠标移出
        /// </summary>
        private void pbLike_MouseLeave(object sender, EventArgs e)
        {
            pbLike.Image = Resources.收藏;
        }

        /// <summary>
        /// 播放列表按钮鼠标停留
        /// </summary>
        private void pbListCount_MouseHover(object sender, EventArgs e)
        {
            pbListCount.Image = Resources.列表hover;
            labelListCount.ForeColor = Color.White;
        }

        /// <summary>
        /// 播放列表按钮鼠标移出
        /// </summary>
        private void pbListCount_MouseLeave(object sender, EventArgs e)
        {
            pbListCount.Image = Resources.列表;
            labelListCount.ForeColor = Color.Silver;
        }

        /// <summary>
        /// 设置的panel顶部，的鼠标移入事件
        /// </summary>
        private void MoveEnter_PanelSeting(object sender, EventArgs e)
        {
            PictureBox currPicBox = (PictureBox)sender;
            if (currPicBox.Name == "pbCloseForm")
            {
                currPicBox.Image = Resources.关闭hoover;
            }
            else if (currPicBox.Name == "pbMaxForm")
            {
                currPicBox.Image = Resources.最大化hoover;
            }
            else if (currPicBox.Name == "pbMinForm")
            {
                currPicBox.Image = Resources.最小化hoover;
            }
            else if (currPicBox.Name == "pbAddSong")
            {
                currPicBox.Image = Resources.添加hoover;
            }
        }

        /// <summary>
        /// 设置的panel顶部，的鼠标停留事件
        /// </summary>
        private void MoveLeave_PanelSeting(object sender, EventArgs e)
        {
            PictureBox currPicBox = (PictureBox)sender;
            if (currPicBox.Name == "pbCloseForm")
            {
                currPicBox.Image = Resources.关闭;
            }
            else if (currPicBox.Name == "pbMaxForm")
            {
                currPicBox.Image = Resources.最大化;
            }
            else if (currPicBox.Name == "pbMinForm")
            {
                currPicBox.Image = Resources.最小化;
            }
            else if (currPicBox.Name == "pbAddSong")
            {
                currPicBox.Image = Resources.添加音乐;
            }
        }

        /// <summary>
        /// 拖动窗口点击
        /// </summary>
        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            downPoint = new Point(e.X, e.Y);
        }

        /// <summary>
        /// 拖动窗口移动
        /// </summary>
        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Location.X + e.X - downPoint.X, this.Location.Y + e.Y - downPoint.Y);
            }
        }

        /// <summary>
        /// 最大化，最小化，关闭按钮点击事件
        /// </summary>
        private void FormControlButton_Click(object sender, EventArgs e)
        {
            PictureBox currPicBox = (PictureBox)sender;
            if (currPicBox.Name == "pbCloseForm")
            {
                this.Close();
            }
            else if (currPicBox.Name == "pbMaxForm")
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else if (currPicBox.Name == "pbMinForm")
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        /// <summary>
        /// listViewcolumn头部列表重绘
        /// </summary>
        private void lvSongList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            /*e.Graphics.FillRectangle(Brushes.WhiteSmoke, e.Bounds);    // 采用特定颜色绘制标题列
            using (StringFormat sf = new StringFormat())
            {
                switch (e.Header.TextAlign)
                {
                    case HorizontalAlignment.Center:
                        sf.Alignment = StringAlignment.Center;
                        break;
                    case HorizontalAlignment.Right:
                        sf.Alignment = StringAlignment.Far;
                        break;
                }

                using (Font headerFont = new Font("Helvetica", 10, FontStyle.Regular))
                {
                    e.Graphics.DrawString(e.Header.Text, headerFont, Brushes.Gray, e.Bounds, sf);
                }
            }*/
            int index = e.ColumnIndex;

            e.Graphics.FillRectangle(new SolidBrush(Color.WhiteSmoke), e.Bounds);
            TextRenderer.DrawText(e.Graphics, lvSongList.Columns[index].Text, new Font("宋体", 10, FontStyle.Regular), e.Bounds, Color.Gray, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            Pen pen = new Pen(Color.FloralWhite, 2);
            Point p = new Point(e.Bounds.Left - 1, e.Bounds.Top + 1);
            Size s = new Size(e.Bounds.Width, e.Bounds.Height - 2);
            Rectangle r = new Rectangle(p, s);
            e.Graphics.DrawRectangle(pen, r);
        }

        /// <summary>
        /// ListView子物体重绘
        /// </summary>
        private void lvSongList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ItemIndex == -1)
            {
                return;
            }

            e.SubItem.ForeColor = Color.DimGray;

            if ((e.ItemState & ListViewItemStates.Selected) == ListViewItemStates.Selected)
            {
                using (SolidBrush brush = new SolidBrush(Color.Blue))
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);
                }
            }

            if (!string.IsNullOrEmpty(e.SubItem.Text))
            {
                this.DrawText(e, e.Graphics, e.Bounds, 2);
            }
        }

        private void DrawText(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r, int paddingLeft)
        {
            TextFormatFlags flags = GetFormatFlags(e.Header.TextAlign);

            r.X += 1 + paddingLeft; // 重绘图标时，文本右移
            TextRenderer.DrawText(
                g,
                e.SubItem.Text,
                e.SubItem.Font,
                r,
                e.SubItem.ForeColor,
                flags);
        }

        private TextFormatFlags GetFormatFlags(HorizontalAlignment align)
        {
            TextFormatFlags flags =
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.VerticalCenter;

            switch (align)
            {
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
                case HorizontalAlignment.Left:
                    flags |= TextFormatFlags.Left;
                    break;
            }

            return flags;
        }

        /// <summary>
        /// 添加音乐的按钮点击事件
        /// </summary>
        private void pbAddSong_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = DefaultSongsFilePath;
            this.openFileDialog1.Filter = "媒体文件|*.mp3;*.wav;*.wma;*.avi;*.mpg;*.asf;*.wmv";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < openFileDialog1.FileNames.Length; i++)
                {
                    string path = openFileDialog1.FileNames[i];
                    if (!IsExistInList(path))
                    {
                        this.localSongsList.Add(new SongsInfo(path));
                    }
                }
            }

            AddSongsToListView(localSongsList);
            SaveSongsListHistory(localSongsFilePath, localSongsList);

            UpdataOringinSongList();
        }

        /// <summary>
        /// 用于判断该歌曲是否在链表中
        /// </summary>
        private bool IsExistInList(string path)
        {
            for (int i = 0; i < localSongsList.Count; i++)
            {
                if (path == localSongsList[i].FilePath)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 添加歌曲到ListView（lvSongList）中
        /// </summary>
        private void AddSongsToListView(List<SongsInfo> songList)
        {
            lvSongList.BeginUpdate();
            lvSongList.Items.Clear();
            foreach (SongsInfo song in songList)
            {
                string[] songAry = new string[7];
                int currCount = lvSongList.Items.Count + 1;
                if (currCount < 10)
                {
                    songAry[0] = "0" + currCount;
                }
                else
                {
                    songAry[0] = "" + currCount;
                }

                songAry[1] = song.FileName;
                songAry[2] = song.Artist;
                songAry[3] = song.Album;
                songAry[4] = song.Duration;
                songAry[5] = song.Filesize;
                songAry[6] = song.Year;

                ListViewItem lvItem = new ListViewItem(songAry);
                lvItem.SubItems.Add(song.FilePath);
                lvSongList.Items.Add(lvItem);

                WMPLib.IWMPMedia media = axWindowsMediaPlayer1.newMedia(song.FilePath);
                axWindowsMediaPlayer1.currentPlaylist.appendItem(media);
            }

            lvSongList.EndUpdate();
        }

        /// <summary>
        /// 保存歌曲列表到本地的历史记录文件
        /// </summary>
        private void SaveSongsListHistory(string savePath, List<SongsInfo> songsList)
        {
            string saveString = "";
            for (int i = 0; i < songsList.Count; i++)
            {
                saveString += songsList[i].FilePath + "};{";
            }

            File.WriteAllText(savePath, saveString);
        }

        /// <summary>
        /// 将历史记录文件读取出来
        /// </summary>
        private List<SongsInfo> ReadHistorySongsList(string filePath)
        {
            List<SongsInfo> resSongList = new List<SongsInfo>();
            string readString = "";
            if (File.Exists(filePath))
            {
                readString = File.ReadAllText(filePath);
                if (readString != "")
                {
                    string[] arr = readString.Split(new string[] { "};{" }, StringSplitOptions.None);
                    foreach (string path in arr)
                    {
                        if (path != null && path != "" && File.Exists(path))
                        {
                            resSongList.Add(new SongsInfo(path));
                        }
                    }
                }
            }
            else
            {
                File.Create(filePath);
            }

            return resSongList;
        }

        /// <summary>
        /// 更新初始的本地音乐lvSongList列表
        /// </summary>
        private void UpdataOringinSongList()
        {
            oringinListSong = new List<SongsInfo>();
            for (int i = 0; i < lvSongList.Items.Count; i++)
            {
                oringinListSong.Add(new SongsInfo(lvSongList.Items[i].SubItems[7].Text));
            }
        }

        /// <summary>
        /// ListView鼠标按下
        /// </summary>
        private void lvSongList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem lvi = lvSongList.GetItemAt(e.X, e.Y);
                if (lvi != null)
                {
                    cmsSongListMenu.Visible = true;
                    currSelectedSong = new SongsInfo(lvi.SubItems[7].Text);
                    cmsSongListMenu.Show(Cursor.Position);
                }
                else
                {
                    cmsSongListMenu.Close();
                }
            }
        }

        /// <summary>
        /// 播放按钮双击，播放选中歌曲
        /// </summary>
        private void lvSongList_DoubleClick(object sender, EventArgs e)
        {
            int currIndex = lvSongList.SelectedItems[0].Index;
            string songFilePath = lvSongList.Items[currIndex].SubItems[7].Text;

            // 选中的歌曲为正在播放的歌曲
            if (currPlaySong.FilePath == songFilePath)
            {
                if (axWindowsMediaPlayer1.playState.ToString() == "wmppsPlaying")
                {
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                    pbPlay.Image = Resources.播放;
                    ttbbtnPlayPause.Icon = Resources.播放1;
                }
                else if (axWindowsMediaPlayer1.playState.ToString() == "wmppsPaused")
                {
                    axWindowsMediaPlayer1.Ctlcontrols.play();
                    pbPlay.Image = Resources.暂停;
                    ttbbtnPlayPause.Icon = Resources.暂停1;
                }
            }
            else
            {
                // 选中的为其他歌曲
                BuildRandomList(lvSongList.Items.Count);
                jumpSongIndex = currIndex;
                currPlaySong = new SongsInfo(songFilePath);
                axWindowsMediaPlayer1.URL = songFilePath;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                pbPlay.Image = Resources.暂停;
                ttbbtnPlayPause.Icon = Resources.暂停1;
            }

            lvSongList.Items[currIndex].Focused = true;
        }

        /// <summary>
        /// 上一首按钮点击
        /// </summary>
        private void pbBack_Click(object sender, EventArgs e)
        {
            if (lvSongList.Items.Count == 0)
            {
                // MessageBox.Show("请先添加曲目至目录");
                return;
            }

            int currIndex = lvSongList.SelectedItems[0].Index;
            if (currIndex > 0)
            {
                axWindowsMediaPlayer1.Ctlcontrols.stop();
                currIndex -= 1;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.stop();
                currIndex = lvSongList.Items.Count - 1;
            }

            lvSongList.Items[currIndex].Focused = true;
            lvSongList.Items[currIndex].EnsureVisible();
            lvSongList.Items[currIndex].Selected = true;

            Play(currIndex);
        }

        /// <summary>
        /// 播放/暂停按钮点击
        /// </summary>
        private void pbPlay_Click(object sender, EventArgs e)
        {
            if (axWindowsMediaPlayer1.playState.ToString() == "wmppsPlaying")
            {
                // 播放->暂停
                axWindowsMediaPlayer1.Ctlcontrols.pause();
                pbPlay.Image = Resources.播放hover;
                ttbbtnPlayPause.Icon = Resources.播放1;
                return;
            }
            else if (axWindowsMediaPlayer1.playState.ToString() == "wmppsPaused")
            {
                // 暂停->播放
                axWindowsMediaPlayer1.Ctlcontrols.play();
                pbPlay.Image = Resources.暂停hover;
                ttbbtnPlayPause.Icon = Resources.暂停1;
                return;
            }

            if (lvSongList.SelectedItems.Count > 0)
            {
                // 双击播放列表控制
                Play(lvSongList.SelectedItems[0].Index);
            }
            else
            {
                MessageBox.Show("请选择要播放的曲目");
            }
        }

        /// <summary>
        /// 下一首按钮点击
        /// </summary>
        private void pbNext_Click(object sender, EventArgs e)
        {
            if (lvSongList.SelectedItems.Count == 0)
            {
                return;
            }

            int currIndex = lvSongList.SelectedItems[0].Index;
            if (currIndex < lvSongList.Items.Count - 1)
            {
                axWindowsMediaPlayer1.Ctlcontrols.stop();
                currIndex += 1;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.stop();
                currIndex = 0;
            }

            Play(currIndex);
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        private void Play(int index)
        {
            // 设置被播放音乐项的状态
            lvSongList.Items[index].Focused = true;
            lvSongList.Items[index].EnsureVisible();
            lvSongList.Items[index].Selected = true;

            if (axWindowsMediaPlayer1.playState.ToString() == "wmppsPlaying")
            {
                axWindowsMediaPlayer1.Ctlcontrols.pause();
                pbPlay.Image = Resources.播放hover;
                ttbbtnPlayPause.Icon = Resources.播放1;
                return;
            }
            else if (axWindowsMediaPlayer1.playState.ToString() != "wmppsPaused")
            {
                // 生成随机序列
                BuildRandomList(lvSongList.Items.Count);
                jumpSongIndex = index;
                currPlaySong = new SongsInfo(lvSongList.SelectedItems[0].SubItems[7].Text);
                axWindowsMediaPlayer1.URL = currPlaySong.FilePath;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                return;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }

            pbPlay.Image = Resources.暂停hover;
            ttbbtnPlayPause.Icon = Resources.暂停1;
            ttbbtnPlayPause.Tooltip = "暂停";
        }

        /// <summary>
        /// 搜索栏选中
        /// </summary>
        private void txtSreachSongName_Enter(object sender, EventArgs e)
        {
            if (txtSreachSongName.Text == "输入要搜索的歌曲名")
            {
                this.txtSreachSongName.Text = "";
            }
        }

        /// <summary>
        /// 搜索栏非选择
        /// </summary>
        private void txtSreachSongName_Leave(object sender, EventArgs e)
        {
            if (txtSreachSongName.Text == "")
            {
                txtSreachSongName.Text = "输入要搜索的歌曲名";
            }
        }

        /// <summary>
        /// 搜索栏字符改变
        /// </summary>
        private void txtSreachSongName_TextChanged(object sender, EventArgs e)
        {
            lbNoResult.SendToBack();

            // 初始化
            if (txtSreachSongName.Text == "")
            {
                switch (lbMenu.SelectedIndex)
                {
                    case 0:
                        AddSongsToListView(localSongsList);
                        break;
                    case 1:
                        AddSongsToListView(favoriteSongsList);
                        break;
                }

                return;
            }
            else
            {
                List<SongsInfo> resultList = new List<SongsInfo>();

                Dictionary<string, SongsInfo> resultDic = new Dictionary<string, SongsInfo>();
                string strSreach = txtSreachSongName.Text;

                // 设置正则匹配
                Regex r = new Regex(Regex.Escape(strSreach), RegexOptions.IgnoreCase);

                for (int i = 0; i < localSongsList.Count; i++)
                {
                    Match m = r.Match(localSongsList[i].FileName);
                    if (m.Success)
                    {
                        resultDic.Add(localSongsList[i].FilePath, localSongsList[i]);
                    }
                }

                for (int i = 0; i < favoriteSongsList.Count; i++)
                {
                    Match m = r.Match(favoriteSongsList[i].FileName);
                    if (m.Success && !resultDic.ContainsKey(localSongsList[i].FilePath))
                    {
                        resultDic.Add(localSongsList[i].FilePath, localSongsList[i]);
                    }
                }

                if (resultDic.Count > 0)
                {
                    List<SongsInfo> resList = new List<SongsInfo>();
                    foreach (SongsInfo song in resultDic.Values)
                    {
                        resList.Add(song);
                    }

                    AddSongsToListView(resList);
                }
                else
                {
                    lvSongList.Items.Clear();

                    // 没有搜索结果
                    lbNoResult.BringToFront();
                }
            }
        }

        /// <summary>
        /// 设置歌曲的滑动条
        /// </summary>
        private void tackBarMove_Scroll(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.currentPosition = (double)this.tackBarMove.Value;
        }

        /// <summary>
        /// 音量滑动条值改变，播放器音量也改变
        /// </summary>
        private void tbMusicVolume_ValueChanged(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.settings.volume = tbMusicVolume.Value;
        }

        /// <summary>
        /// 系统托盘显示主菜单
        /// </summary>
        private void tsmiShowMain_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// 系统托盘退出
        /// </summary>
        private void tsmiQuit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            this.Close();
        }

        /// <summary>
        /// 系统托盘双击
        /// </summary>
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Visible = true;
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// 收藏音乐
        /// </summary>
        private void tsmiFavorite_Click(object sender, EventArgs e)
        {
            foreach (SongsInfo song in favoriteSongsList)
            {
                if (currSelectedSong.FilePath == song.FilePath)
                {
                    return;
                }
            }

            favoriteSongsList.Add(new SongsInfo(currSelectedSong.FilePath));
            SaveSongsListHistory(favoriteSongsFilePath, favoriteSongsList);
        }

        /// <summary>
        /// 从列表中删除
        /// </summary>
        private void tsmiRemoveSongFromList_Click(object sender, EventArgs e)
        {
            DeleteSongFormList deleteSongFormList = new DeleteSongFormList(currSelectedSong.FilePath);
            if (deleteSongFormList.ShowDialog() == DialogResult.OK)
            {
                int removeIndex = lvSongList.SelectedItems[0].Index;
                if (lbMenu.SelectedIndex == 0)
                {
                    localSongsList.RemoveAt(removeIndex);
                    SaveSongsListHistory(localSongsFilePath, localSongsList);
                    AddSongsToListView(localSongsList);
                }
                else if (lbMenu.SelectedIndex == 1)
                {
                    favoriteSongsList.RemoveAt(removeIndex);
                    SaveSongsListHistory(favoriteSongsFilePath, favoriteSongsList);
                    AddSongsToListView(favoriteSongsList);
                }

                UpdataOringinSongList();
            }
        }

        /// <summary>
        /// 打开文件位置
        /// </summary>
        private void tsmiOpenFilePath_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"Explorer.exe", "/select,\"" + currSelectedSong.FilePath + "\"");
        }

        /// <summary>
        /// 左边的菜单ListBox的子物体绘制
        /// </summary>
        private void lbMenu_DrawItem(object sender, DrawItemEventArgs e)
        {
            Bitmap bitmap = new Bitmap(e.Bounds.Width, e.Bounds.Height);

            int index = e.Index;                                // 获取当前要进行绘制的行的序号，从0开始。
            Graphics g = e.Graphics;                            // 获取Graphics对象。

            Graphics tempG = Graphics.FromImage(bitmap);

            tempG.SmoothingMode = SmoothingMode.AntiAlias;          // 使绘图质量最高，即消除锯齿
            tempG.InterpolationMode = InterpolationMode.HighQualityBicubic;
            tempG.CompositingQuality = CompositingQuality.HighQuality;

            Rectangle bound = e.Bounds;                         // 获取当前要绘制的行的一个矩形范围。
            string text = this.menuItemList[index].Text.ToString();     // 获取当前要绘制的行的显示文本。

            // 绘制选中时的背景，要注意绘制的顺序，后面的会覆盖前面的
            // 绘制底色
            Color backgroundColor = Color.FromArgb(34, 35, 39);             // 背景色
            Color guideTagColor = Color.FromArgb(183, 218, 114);            // 高亮指示色
            Color selectedBackgroundColor = Color.FromArgb(46, 47, 51);     // 选中背景色
            Color fontColor = Color.Gray;                                   // 字体颜色
            Color selectedFontColor = Color.White;                          // 选中字体颜色
            Font textFont = new Font("微软雅黑", 9, FontStyle.Bold);        // 文字
            Image itmeImage = this.menuItemList[index].Img;            // 图标

            // 矩形大小
            Rectangle backgroundRect = new Rectangle(0, 0, bound.Width, bound.Height);
            Rectangle guideRect = new Rectangle(0, 4, 5, bound.Height - 8);
            Rectangle textRect = new Rectangle(55, 0, bound.Width, bound.Height);
            Rectangle imgRect = new Rectangle(20, 4, 22, bound.Height - 8);

            // 当前选中行
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                backgroundColor = selectedBackgroundColor;
                fontColor = selectedFontColor;
            }
            else
            {
                guideTagColor = backgroundColor;
            }

            // 绘制背景色
            tempG.FillRectangle(new SolidBrush(backgroundColor), backgroundRect);

            // 绘制左前高亮指示
            tempG.FillRectangle(new SolidBrush(guideTagColor), guideRect);

            // 绘制显示文本
            TextRenderer.DrawText(tempG, text, textFont, textRect, fontColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            // 绘制图标
            tempG.DrawImage(itmeImage, imgRect);

            g.DrawImage(bitmap, bound.X, bound.Y, bitmap.Width, bitmap.Height);
            tempG.Dispose();
        }

        /// <summary>
        /// 设置listBox的item的高度
        /// </summary>
        private void lbMenu_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 30;
        }

        /// <summary>
        /// 左边菜单选择操作
        /// </summary>
        private void lbMenu_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (lbMenu.SelectedIndex)
            {
                case 0:
                    lvSongList.Items.Clear();
                    AddSongsToListView(localSongsList);

                    lvSongList.BringToFront();
                    tsmiFavorite.Visible = true;
                    pbAddSong.Visible = true;
                    break;
                case 1:
                    lvSongList.Items.Clear();
                    AddSongsToListView(favoriteSongsList);

                    lvSongList.BringToFront();
                    tsmiFavorite.Visible = false;
                    pbAddSong.Visible = false;
                    break;
            }

            int songsCount = lvSongList.Items.Count;
            lvSongList.Columns[0].Text = songsCount.ToString();
        }

        /// <summary>
        /// 记录当前歌曲播放时间
        /// </summary>
        private void timerPlay_Tick(object sender, EventArgs e)
        {
            // 设置当前播放时间
            labelMusicTimer.Text = axWindowsMediaPlayer1.Ctlcontrols.currentPositionString + " / " + currPlaySong.Duration.Remove(0, 3);

            // 设置滑动条值
            tackBarMove.Value = (int)axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
        }
    }
}
