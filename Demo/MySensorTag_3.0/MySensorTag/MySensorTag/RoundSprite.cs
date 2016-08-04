using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySensorTag
{
    public class RoundSprite
    {
        private double width;
        private double height;

        public double x, y, radius;
        protected double speedX, speedY;
        public RoundSprite(double x, double y, double radius, double speedX, double speedY)
        {
            this.x = x;
            this.y = y;
            this.radius = radius;
            this.speedX = speedX;
            this.speedY = speedY;
            width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
        }

        void move()
        {
            x += speedX;
            y += speedY;
        }

        void checkBoundaries()
        {
            if (x > width - radius)
            {
                x = width - radius;
                speedX *= -1;

            }
            if (x < radius)
            {
                x = radius;
                speedX *= -1;
            }
            if (y > height - radius)
            {
                y = height - radius;
                speedY *= -1;
            }
            if (y < radius)
            {
                y = radius;
                speedY *= -1;
            }
        }
    }
}
