# WinKnight Project Overview

## Vision
**WinKnight** is an intelligent, self-healing system recovery solution designed for Windows users.  
Its core mission is to provide a stable, reliable PC experience by proactively managing system health and automatically resolving common issues without manual intervention.  
The project aims to simplify PC maintenance, making advanced system diagnostics and repair accessible to everyone.

---

## High-Level Workflow
The project is built on an automated, continuous feedback loop:

1. **System Monitoring**  
   WinKnight operates silently in the background, continuously monitoring for signs of trouble.

2. **Proactive Backup**  
   The **RestoreGuard** module proactively watches for significant system changes, such as Windows Updates, and automatically creates a system restore point before changes are applied.

3. **Issue Detection**  
   The **SelfHeal** module analyzes system event logs and other key metrics to detect warnings, errors, and potential instabilities.

4. **Automated Repair**  
   If issues are found, the **SelfHeal** module executes built-in Windows repair tools like **SFC** and **DISM**.  
   It also runs the **CacheCleaner** module to clear out temporary files that could be causing problems.

5. **Recovery Fallback**  
   If the automated repair fails or system stability remains a concern, the system can automatically restore from the most recent, known-good restore point created by the application.

6. **User Reporting**  
   A clean, user-friendly report is generated to show:
   - What was found  
   - What was fixed  
   - The current system status

7. **Continuous Loop**  
   The system then returns to monitoring, creating a continuous loop of proactive system care.

---

## Tech Stack and Tools
**Programming Languages:**  
- C# (leveraging the full .NET Framework for Windows-specific APIs)  
- PowerShell  

**Core Modules:**  
- **SelfHeal**: C# executable performing system diagnostics and repairs using SFC, DISM, and driver integrity checks.  
- **CacheCleaner**: C# executable clearing temporary files from `%temp%`, Prefetch, and other locations.  
- **RestoreGuard**: C# executable using WMI to watch for system events and create restore points.

**Windows APIs & Services:**  
- Volume Shadow Copy Service (VSS)  
- Windows Management Instrumentation (WMI)  
- Task Scheduler API  
- Event Log API  

**UI Framework:**  
- (Planned) Modern Windows UI framework like **WinUI 3** or **WPF**  

**Build & Deployment:**  
- Visual Studio IDE  
- MSIX Packaging  

---

## Development and Execution

| Week | Activity |
|------|----------|
| **1** | Requirements, design, environment setup |
| **2** | Development of the RestoreGuard module |
| **3** | Development and refinement of the SelfHeal and CacheCleaner modules |
| **4** | UI development, module integration, comprehensive testing |

---

## Prerequisites
**Knowledge:**  
- Windows system architecture  
- C# development  
- Familiarity with Windows repair tools (SFC, DISM)  
- Software development best practices  

**Software:**  
- Visual Studio 2022+ (with .NET desktop development workload)  
- Windows 10/11 OS  

---

## Important Notes
- This application **requires administrator privileges** to function correctly.  
- All C# modules (SelfHeal, CacheCleaner) explicitly request administrator permissions via an application manifest file.  
- The SelfHeal module performs a **diagnostic scan first** and will only trigger repairs if a problem is detected.
