WinKnight Project Overview
Vision
WinKnight is an intelligent, self-healing system recovery solution designed for Windows users. Its core mission is to provide a stable, reliable PC experience by proactively managing system health and automatically resolving common issues without manual intervention. The project aims to simplify PC maintenance, making advanced system diagnostics and repair accessible to everyone.

High-Level Workflow
The project is built on an automated, continuous feedback loop:

System Monitoring: WinKnight operates silently in the background, continuously monitoring for signs of trouble.

Proactive Backup: The RestoreGuard module proactively watches for significant system changes, such as Windows Updates, and automatically creates a system restore point for safety before changes are applied.

Issue Detection: The SelfHeal module analyzes system event logs and other key metrics to detect warnings, errors, and potential instabilities.

Automated Repair: If issues are found, the SelfHeal module executes built-in Windows repair tools like SFC and DISM. It also runs the CacheCleaner module to clear out temporary files that could be causing problems.

Recovery Fallback: If the automated repair fails or the system's stability remains a concern, the system can automatically initiate a restore from the most recent, known-good restore point created by the application.

User Reporting: A clean, user-friendly report is generated to show what was found, what was fixed, and the current system status.

Continuous Loop: The system then returns to monitoring, creating a continuous loop of proactive system care.

Tech Stack and Tools
Programming Languages: C# (leveraging the full .NET Framework for Windows-specific APIs) and PowerShell.

Core Modules:

SelfHeal: A C# executable that performs system diagnostics and repairs using SFC, DISM, and driver integrity checks.

CacheCleaner: A C# executable that clears temporary files from common locations like %temp% and Prefetch.

RestoreGuard: A C# executable that uses Windows Management Instrumentation (WMI) to watch for system events and create system restore points.

Windows APIs & Services: Volume Shadow Copy Service (VSS), Windows Management Instrumentation (WMI), Task Scheduler API, Event Log API.

UI Framework: (To be developed) The project's front-end will be built using a modern Windows UI framework like WinUI 3 or WPF.

Build & Deployment: Visual Studio IDE, MSIX Packaging.

Development and Execution
This project is structured for a team of 3 people with a one-month timeline, focusing on a sequential development process.

Week

Activity

1

Requirements, design, environment setup.

2

Development of the RestoreGuard module.

3

Development and refinement of the SelfHeal and CacheCleaner modules.

4

UI development, module integration, comprehensive testing.

Prerequisites
Knowledge: Windows system architecture, C# development, familiarity with Windows repair tools (SFC, DISM), and software development practices.

Software: Visual Studio 2022+ (with the .NET desktop development workload), Windows 10/11 OS.

Important Notes
This application requires administrator privileges to function correctly.

All C# modules, such as SelfHeal and CacheCleaner, have been built to explicitly request administrator permissions upon launch via an application manifest file.

The SelfHeal module is designed to perform a diagnostic scan first and will only trigger a repair action if a problem is explicitly detected.
