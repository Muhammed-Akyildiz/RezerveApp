import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Business\Appointments.cshtml'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_block = '''                            @if (a.Status == "Approved" || a.Status == "Pending")
                            {
                                <a href="@waLink" target="_blank" class="btn btn-ghost btn-sm" style="color:#25D366; border-color:transparent;" title="WhatsApp'tan Hatırlat">
                                    <svg class="icon-sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
                                    WhatsApp
                                </a>
                            }'''

new_block = '''                            @if (a.Status == "Approved" || a.Status == "Pending")
                            {
                                <a href="@waLink" target="_blank" class="btn btn-ghost btn-sm" style="color:#25D366; border-color:transparent;" title="WhatsApp'tan Hatırlat">
                                    <svg class="icon-sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
                                    WhatsApp
                                </a>
                            }
                            else if (a.Status == "Completed")
                            {
                                var reviewUrl = Url.Action("Leave", "Review", new { appointmentId = a.Id }, Context.Request.Scheme);
                                var reviewWaText = System.Net.WebUtility.UrlEncode($"RezerveApp üzerinden aldığınız hizmet tamamlandı. Bizi değerlendirmek ister misiniz? Link: {reviewUrl}");
                                var reviewWaLink = $"https://wa.me/{waPhone}?text={reviewWaText}";

                                <a href="@reviewWaLink" target="_blank" class="btn btn-ghost btn-sm" style="color:#d4af37; border-color:transparent;" title="Değerlendirme İste">
                                    <svg class="icon-sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
                                    Değerlendirme İste
                                </a>
                            }'''

content = content.replace(old_block, new_block)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)
