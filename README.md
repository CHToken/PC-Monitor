# PC Monitor

**Intelligent process manager for Windows 11.** Built by TechyTro Software Development.

---

## What It Does

PC Monitor gives you full visibility into every process running on your Windows PC so you know exactly what's consuming your CPU, RAM, and making your fans spin up. It goes beyond Task Manager by telling you **what each process actually does** and **whether it's safe to terminate**.

### Features

- **Real-time system dashboard** — CPU %, RAM usage, disk space, uptime — all updating every 3 seconds
- **Smart process intelligence** — recognizes 50+ common Windows processes and tells you what they do (Chrome, VS Code, PostgreSQL, Defender, Node, Nginx, etc.)
- **Safety ratings** for every process:
  - Green = **Safe** — you can close this without issues
  - Orange = **Caution** — check what it's doing first
  - Red = **Critical** — system process, protected from accidental termination
- **Kill with confidence** — terminate safe/caution processes with a confirmation dialog. Critical processes are protected.
- **Category filters** — quickly find all browsers, IDEs, databases, or background services
- **Search** — type a name or PID to find a specific process instantly
- **Sort by CPU or RAM** — instantly see what's hogging resources

### Use Cases

| Scenario | What to do |
|---|---|
| Laptop fan going crazy | Open PC Monitor, sort by CPU, kill heavy browser tabs |
| RAM at 90% | Filter by "Safe" risk, terminate unused apps |
| Wondering what a process is | Click it — description and notes appear |
| After closing VS Code | Check that language servers exited cleanly |
| PostgreSQL eating CPU | Identify and manage database processes |

---

## How to Run

Double-click `PC-Monitor.exe` — that's it. No installation, no dependencies.

---

## Source Code

The full C# source is in the `src/` folder. To rebuild from source:

```bash
cd src
dotnet publish -c Release -o ../
```

Built with:
- .NET 8 (Windows Forms)
- Single-file self-contained publish (no .NET runtime needed on target machine)

---

## Support

- Found a bug or have a feature idea? Open an [issue](../../issues) on GitHub.
- Want to contribute? Fork the repo, make your changes, and submit a pull request. All contributions are welcome — see `src/Program.cs` for the codebase.
- Questions? Reach out to TechyTro Software Development at techytrosoftware@gmail.com.

---

## License

MIT — see [LICENSE](LICENSE) for full terms. You are free to use, modify, distribute, and build upon this software for any purpose.

---

Built by **TechyTro Software Development** (techytrosoftware@gmail.com)
