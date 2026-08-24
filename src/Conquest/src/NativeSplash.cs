using System;
using System.Windows.Forms;
using Bitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;

namespace ConquestFrontierWarsRay;

internal sealed class NativeSplash : IDisposable
{
    private readonly Bitmap image;
    private readonly Form form;
    private bool disposed;

    private NativeSplash(Bitmap image, Form form)
    {
        this.image = image;
        this.form = form;
    }

    public static NativeSplash? TryShow(string? path)
    {
        if (path is null)
        {
            return null;
        }

        try
        {
            Bitmap image = new(path);
            Form form = new()
            {
                AutoScaleMode = AutoScaleMode.None,
                BackColor = DrawingColor.Black,
                ClientSize = image.Size,
                FormBorderStyle = FormBorderStyle.None,
                ShowIcon = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true
            };

            PictureBox pictureBox = new()
            {
                Dock = DockStyle.Fill,
                Image = image,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            form.Controls.Add(pictureBox);
            form.Show();
            Application.DoEvents();
            return new NativeSplash(image, form);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (!form.IsDisposed)
        {
            form.Close();
            form.Dispose();
            Application.DoEvents();
        }

        image.Dispose();
    }
}
