$canonicalFooter = @"
<footer>
  <div class="footer-inner">
    <div class="footer-brand">
      <a href="/" style="display:flex;align-items:center;gap:12px;margin-bottom:14px;text-decoration:none;">
        <img src="/images/logo.png" alt="NAU Logo" style="height:48px;width:auto;filter:brightness(0) invert(1);" onerror="this.style.display='none'">
        <span style="font-size:20px;font-weight:700;color:#fff;font-family:'Poppins',sans-serif;white-space:nowrap;">National Aviation University</span>
      </a>
      <p>A premier institution dedicated to excellence in aviation education, training, research and innovation.</p>
      <div class="social-links">
        <a href="https://www.facebook.com/CATI.PK/" target="_blank" rel="noopener noreferrer" title="Facebook" aria-label="Facebook"><i class="fab fa-facebook-f"></i></a>
        <a href="https://www.instagram.com/cati.gov.pk?igsh=NzE1anhjOGJkemNh" target="_blank" rel="noopener noreferrer" title="Instagram" aria-label="Instagram"><i class="fab fa-instagram"></i></a>
        <a href="https://www.linkedin.com/company/civil-aviation-training-institute-cati/posts/" target="_blank" rel="noopener noreferrer" title="LinkedIn" aria-label="LinkedIn"><i class="fab fa-linkedin-in"></i></a>
        <a href="https://www.youtube.com/@CATIHyderabad" target="_blank" rel="noopener noreferrer" title="YouTube" aria-label="YouTube"><i class="fab fa-youtube"></i></a>
        <a href="https://wa.me/92229260310" target="_blank" rel="noopener noreferrer" title="WhatsApp" aria-label="WhatsApp"><i class="fab fa-whatsapp"></i></a>
      </div>
    </div>
    <div class="footer-col">
      <h4>Quick Links</h4>
      <ul>
        <li><a href="/about.html">About Us</a></li>
        <li><a href="/Faculties/Public">Academics</a></li>
        <li><a href="/Account/Register">Admissions</a></li>
        <li><a href="/SimulationLabs/Public">Training</a></li>
        <li><a href="/facilities.html">Campus Life</a></li>
        <li><a href="/Contact">Contact Us</a></li>
      </ul>
    </div>
    <div class="footer-col">
      <h4>Contact Us</h4>
      <div class="contact-item"><i class="fas fa-map-marker-alt"></i><span>Airport Road, Hyderabad Sindh, Pakistan</span></div>
      <div class="contact-item"><i class="fas fa-phone"></i><span>+92 22 9260310-19</span></div>
      <div class="contact-item"><i class="fas fa-envelope"></i><span>AdID.Academics@paa.gov.pk</span></div>
      <div class="contact-item"><i class="fas fa-globe"></i><span>www.cati.edu.pk</span></div>
    </div>
  </div>
  <div class="footer-bottom">
    <span style="white-space:nowrap;">&copy; 2026 National Aviation University. All Rights Reserved.</span>
    <div class="footer-bottom-links">
      <a href="#">Privacy Policy</a>
      <a href="#">Terms of Use</a>
      <a href="#">Sitemap</a>
    </div>
  </div>
</footer>
"@

# Update SeedData html files
$files = Get-ChildItem -Path "c:\NAU_CMS\NAU_\SeedData\*.html"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    if ($content -match '(?s)<footer\b[^>]*>.*?</footer>') {
        $updated = [System.Text.RegularExpressions.Regex]::Replace($content, '(?s)<footer\b[^>]*>.*?</footer>', $canonicalFooter)
        [System.IO.File]::WriteAllText($f.FullName, $updated, [System.Text.Encoding]::UTF8)
        Write-Output "Updated SeedData file: $($f.Name)"
    }
}

# Update Database Pages table
$sqlScript = @"
DECLARE @footer NVARCHAR(MAX) = N'$($canonicalFooter.Replace("'", "''"))';

-- Update all pages in Pages table
UPDATE Pages
SET ContentHtml = 
    CASE 
        WHEN CHARINDEX('<footer', ContentHtml) > 0 AND CHARINDEX('</footer>', ContentHtml) > 0 THEN
            SUBSTRING(ContentHtml, 1, CHARINDEX('<footer', ContentHtml) - 1) + 
            @footer + 
            SUBSTRING(ContentHtml, CHARINDEX('</footer>', ContentHtml) + 9, LEN(ContentHtml))
        ELSE ContentHtml
    END
WHERE ContentHtml LIKE '%<footer%</footer>%';
"@

$sqlScript | Out-File -FilePath "c:\NAU_CMS\NAU_\SeedData\update_db_footers.sql" -Encoding UTF8
sqlcmd -S "localhost\SQLEXPRESS" -d "NAU_CMS" -E -i "c:\NAU_CMS\NAU_\SeedData\update_db_footers.sql"
Write-Output "Database Pages footers updated!"
