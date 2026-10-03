using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {
        var existingAppointments = new List<Tuple<TimeSpan, TimeSpan>>
        {
            Tuple.Create(new TimeSpan(15, 30, 0), new TimeSpan(16, 00, 0))
        };
        
        var dayStart = new TimeSpan(9, 0, 0);
        var dayEnd = new TimeSpan(19, 0, 0);
        var current = dayStart;
        var duration = TimeSpan.FromMinutes(20);
        var slotInterval = 30;
        
        while (current.Add(duration) <= dayEnd)
        {
            var slotEnd = current.Add(duration);
            var conflict = existingAppointments.Any(a => current < a.Item2 && slotEnd > a.Item1);
            
            if (current >= new TimeSpan(14, 30, 0) && current <= new TimeSpan(16, 30, 0))
            {
                Console.WriteLine($"{current} to {slotEnd} - Conflict: {conflict}");
            }
            
            current = current.Add(TimeSpan.FromMinutes(slotInterval));
        }
    }
}
