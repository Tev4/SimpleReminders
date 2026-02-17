Add-Type -AssemblyName System.Windows.Forms, System.Drawing, System.Media

# --- SOUND ---
$soundPath = "C:\Windows\Media\Windows Notify Messaging.wav"
$player = New-Object System.Media.SoundPlayer($soundPath)
$player.Play()

# --- SINGLE INSTANCE CHECK ---
$currentPID = $PID
$scriptName = $MyInvocation.MyCommand.Name
$alreadyRunning = Get-CimInstance Win32_Process -Filter "name = 'powershell.exe' OR name = 'pwsh.exe'" | 
    Where-Object { $_.CommandLine -like "*$scriptName*" -and $_.ProcessId -ne $currentPID }

if ($alreadyRunning) { 
    # We close the OLD one.
    $alreadyRunning | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
}

# --- DEFINE CUSTOM "GHOST" FORM ---
# We override CreateParams to set NOACTIVATE and TOPMOST at the kernel level.
if (-not ([System.Management.Automation.PSTypeName]'GhostForm').Type) {
    Add-Type -TypeDefinition @"
        using System;
        using System.Windows.Forms;
        public class GhostForm : Form
        {
            // 1. Tell Windows this window should not be activated when shown
            protected override bool ShowWithoutActivation
            {
                get { return true; }
            }

            // 2. Set low-level window styles
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    // WS_EX_TOPMOST (0x8)     : Always on top
                    // WS_EX_TOOLWINDOW (0x80) : Hides from Alt-Tab
                    // WS_EX_NOACTIVATE (0x8M) : Prevents focus grabbing
                    cp.ExStyle |= 0x08000088; 
                    return cp;
                }
            }
        }
"@ -ReferencedAssemblies System.Windows.Forms
}

# --- CONFIGURATION ---
$width = 180
$height = 54
$buttonText = "Drink Water"
$cornerRadius = 24
$accentColor = [Drawing.Color]::FromArgb(0, 95, 184)

$fontName = "Segoe UI Variable Display"
if (-not (New-Object Drawing.FontFamily($fontName))) { $fontName = "Segoe UI" }
$uiFont = New-Object Drawing.Font($fontName, 11, [Drawing.FontStyle]::Bold)

# --- FORM SETUP ---
$form = New-Object GhostForm
$form.Size = New-Object Drawing.Size($width, $height)
$form.FormBorderStyle = "None"
# Note: We do NOT set TopMost here anymore, it is handled in the C# class above.
$form.ShowInTaskbar = $false
$form.BackColor = $accentColor

$screen = [Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form.StartPosition = "Manual"
$form.Location = New-Object Drawing.Point(
    ($screen.Width - $width - 20), 
    ($screen.Height - $height - 20)
)

# Rounded Corners
$path = New-Object Drawing.Drawing2D.GraphicsPath
$rect = New-Object Drawing.Rectangle(0, 0, $width, $height)
$size = $cornerRadius
$path.AddArc($rect.X, $rect.Y, $size, $size, 180, 90)
$path.AddArc(($rect.X + $rect.Width - $size), $rect.Y, $size, $size, 270, 90)
$path.AddArc(($rect.X + $rect.Width - $size), ($rect.Y + $rect.Height - $size), $size, $size, 0, 90)
$path.AddArc($rect.X, ($rect.Y + $rect.Height - $size), $size, $size, 90, 90)
$path.CloseAllFigures()
$form.Region = [System.Drawing.Region]::new($path)

# --- BUTTON SETUP ---
$btn = New-Object Windows.Forms.Button
$btn.Text = $buttonText
$btn.Dock = "Fill"
$btn.FlatStyle = "Flat"
$btn.FlatAppearance.BorderSize = 0
$btn.ForeColor = [Drawing.Color]::White
$btn.Font = $uiFont
$btn.Cursor = [Windows.Forms.Cursors]::Hand
# Important: Stop the button from accepting Tab focus
$btn.TabStop = $false 

# This hides the focus rectangle
$btn.GetType().GetMethod("SetStyle", [Reflection.BindingFlags]"Instance, NonPublic").Invoke($btn, @([Windows.Forms.ControlStyles]::Selectable, $false))

$btn.Add_Click({ $form.Close() })
$form.Controls.Add($btn)

# --- SHOW FORM ---
# We use Application.Run() to start the loop, but we do NOT use ShowDialog().
$form.Show()
[System.Windows.Forms.Application]::Run($form)