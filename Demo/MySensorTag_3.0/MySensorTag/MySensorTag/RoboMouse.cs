using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MySensorTag
{
    public class RoboMouse: RoundSprite
    {
        float localX, localY;

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        public RoboMouse(double x, double y, double radius, double speedX, double speedY): base(x,y,radius,speedX,speedY)
        {
            localX = Cursor.Position.X;
            localY = Cursor.Position.Y;
       
        }

        public void move()
        {
            x += speedX;
            y += speedY;
            SetCursorPos(Cursor.Position.X+(int)x, Cursor.Position.Y+(int)y);
            //  print("x: "); print(frame.getLocation().x+(int)x); print("; y: "); println(frame.getLocation().y+(int)y);
        }
    }
}
