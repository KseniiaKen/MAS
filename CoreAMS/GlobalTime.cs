using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS
{
    // для управления временем системы
    public static class GlobalTime
    {
        private static int time = 0;
        private static int day = Enums.HoursDay;
        private static int delay = 10;

        public static int realTime // метод, позволяющий получить время в текущий день
        {
            get { return time % 24; }
        }

        // Количество часов с момента запуска системы
        public static int Time
        {
            get { return time; }
            set { time = value; }
        }

        // Количество дней с момента запуска системы
        public static int Day
        {
            get { return time / day; }
        }

        // Возвращает true, если настал новый день
        public static bool isNextDay
        {
            get { return (time + 1) % Enums.HoursDay == 0; }
        }
    }
}
