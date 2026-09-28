import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Controllers\BusinessController.cs'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix Daily
old_daily = '''                var query = await _context.Appointments
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate.Date && x.AppointmentDate <= targetDate.Date.AddDays(1))
                    .Select(x => new { Date = x.AppointmentDate.Date, Price = x.Service != null ? x.Service.Price : 0 })
                    .GroupBy(x => x.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(a => a.Price) })
                    .ToListAsync();'''

new_daily = '''                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate.Date && x.AppointmentDate <= targetDate.Date.AddDays(1))
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => x.AppointmentDate.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();'''

content = content.replace(old_daily, new_daily)

# Fix Yearly
old_yearly = '''                var query = await _context.Appointments
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .Select(x => new { Year = x.AppointmentDate.Year, Price = x.Service != null ? x.Service.Price : 0 })
                    .GroupBy(x => x.Year)
                    .Select(g => new { Year = g.Key, Revenue = g.Sum(a => a.Price) })
                    .ToListAsync();'''

new_yearly = '''                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => x.AppointmentDate.Year)
                    .Select(g => new { Year = g.Key, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();'''

content = content.replace(old_yearly, new_yearly)

# Fix Monthly
old_monthly = '''                var query = await _context.Appointments
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .Select(x => new { Year = x.AppointmentDate.Year, Month = x.AppointmentDate.Month, Price = x.Service != null ? x.Service.Price : 0 })
                    .GroupBy(x => new { x.Year, x.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(a => a.Price) })
                    .ToListAsync();'''

new_monthly = '''                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => new { x.AppointmentDate.Year, x.AppointmentDate.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();'''

content = content.replace(old_monthly, new_monthly)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Memory-based grouping logic applied.")
