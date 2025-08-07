# WinKnight Project Overview

## Vision  
WinKnight aims to provide every Windows user, from beginners to experts, with a seamless, intelligent, and fully automated system recovery solution. By proactively creating system restore points and silently diagnosing and repairing common Windows issues, WinKnight empowers users to enjoy a stable, reliable, and self-healing PC experience without manual intervention or technical expertise.

---

## High-Level Workflow / Flowchart Description

1. **Start**  
   - Application or Windows boots up.

2. **Check Conditions for Restore Point Creation**  
   - Is it scheduled time?  
   - Has a system-changing event occurred (e.g., software install, driver update)?  
   - Is the last restore point too old?

3. **Create System Restore Point**  
   - Invoke Windows Backup APIs or PowerShell to create a restore point.  
   - Log success or failure.

4. **Monitor System Health**  
   - Continuously monitor Windows Event Logs, Performance Counters, Crash Dumps.  
   - Detect warnings and errors that could lead to system instability.

5. **Detect Issue?**  
   - Yes → Proceed to automatic repair step.  
   - No → Continue monitoring.

6. **Run Automated Repair**  
   - Execute System File Checker (SFC), DISM, driver verification, malware scan scripts silently.  
   - Apply fixes or roll back recent changes based on health status.

7. **Verification Post-Repair**  
   - Check if the issue is resolved.  
   - If resolved, log and notify user with minimal info or silently.

8. **Recovery Fallback**  
   - If repair fails or system instability continues, automatically initiate system restore using the latest restore point.

9. **User Notification (Optional)**  
   - Provide a user-friendly report/dashboard with what was done and system status.

10. **Loop back to Monitoring**

---

## Tech Stack and Tools

- **Programming Languages:**  
  - C# (.NET 6/7)  
  - PowerShell scripting  

- **Windows APIs & Services:**  
  - Volume Shadow Copy Service (VSS)  
  - Windows Management Instrumentation (WMI)  
  - Task Scheduler API  
  - Event Log API  

- **UI Framework:**  
  - WinUI 3 or WPF  

- **Automation and Repair Tools:**  
  - System File Checker (SFC)  
  - Deployment Image Servicing and Management (DISM)  

- **Build & Deployment:**  
  - Visual Studio IDE  
  - MSIX Packaging or MSI Installer tools  

- **Optional:**  
  - Azure Application Insights  

---

## Development and Execution Timeline (1 Month)

| Week | Activity                                              | Hours Estimate |
|------|-------------------------------------------------------|----------------|
| 1    | Requirements, design, environment setup               | 20             |
| 2    | Restore Point Automation module                       | 25             |
| 3    | Automated Repair scripts and background service       | 35             |
| 4    | Monitoring, UI development, integration, testing      | 50             |
| 5    | Final polishing, documentation, buffer                | 30             |

**Total:** ~160–180 hours  
**Team Size:** 3 people working sequentially at 2 hours/day = 180 hours total

---

## Knowledge and Software Prerequisites

- **Knowledge:**  
  - Windows system internals and architecture  
  - Windows API & PowerShell scripting  
  - C# development and Windows Services  
  - Familiarity with SFC, DISM, and event log analysis  
  - Software development practices (version control, testing)

- **Software:**  
  - Microsoft Visual Studio 2022+  
  - PowerShell 5.1+  
  - Windows 10/11 OS  
  - Windows SDK  
  - MSIX Packaging Tool or WiX  
  - Windows Debugging Tools (WinDbg)

---

## Additional Notes

- Administrator privileges are required for restore points and repair commands.  
- Compatibility testing across multiple Windows versions is essential.  
- Clear documentation and project management are key for sequential collaboration.
