"""Simple source-level acceptance checks. Does NOT compile or execute .NET/SQL Server."""
from pathlib import Path
import json, re

root = Path(__file__).resolve().parents[1]
needed = [
    'Program.cs', 'StudentSupport.csproj', 'Properties/launchSettings.json',
    'Controllers/TicketsController.cs', 'Controllers/IntakeController.cs',
    'Controllers/AdminController.cs', 'Controllers/DemoController.cs',
    'Controllers/AccountController.cs', 'Services/DemoSeeder.cs',
    'Services/SecurityService.cs', 'Services/AttachmentService.cs',
    'Models/Entities.cs', 'Data/AppDbContext.cs',
    'Views/Demo/Index.cshtml', 'Views/Intake/Queue.cshtml',
    'Views/Tickets/Details.cshtml', 'Views/Admin/Audit.cshtml',
    'wwwroot/css/site.css', 'sql/upgrade_v1_to_v2.sql',
    'sql/least_privilege.sql', 'sql/sql_server_audit_optional.sql',
    'DOCS_PLAN.md', 'tests/TEST_CASES.md', 'demo_assets/fake.pdf',
]
checks = {}
checks['Required deliverable files'] = all((root / file).is_file() for file in needed)
for file in ['appsettings.json', 'Properties/launchSettings.json']:
    try:
        json.loads((root / file).read_text())
        checks[f'Valid JSON: {file}'] = True
    except Exception:
        checks[f'Valid JSON: {file}'] = False
code = '\n'.join(p.read_text(encoding='utf-8') for p in (root / 'Controllers').glob('*.cs'))
view = '\n'.join(p.read_text(encoding='utf-8') for p in (root / 'Views').rglob('*.cshtml'))
checks['No unsafe Html.Raw calls in Razor'] = 'Html.Raw(' not in view
checks['ClosedUtc nullable assignment compile guard'] = 'UpdatedUtc = t.ClosedUtc =' not in code and 't.UpdatedUtc = closedAt;' in code and 't.ClosedUtc = closedAt;' in code
checks['All POST forms protected via global AutoValidateAntiforgery'] = 'AutoValidateAntiforgeryTokenAttribute' in (root / 'Program.cs').read_text()
checks['New queues/transfer/status methods are present'] = all(f in code for f in ['Triage(', 'Transfer(', 'Assign(', 'Reopen(', 'Status(', 'Download(', 'Upload('])
checks['Student code FK declared'] = 'HasForeignKey(x => x.StudentCode)' in (root / 'Data/AppDbContext.cs').read_text()
checks['Files written outside wwwroot'] = '"App_Data", "attachments"' in (root / 'Services/AttachmentService.cs').read_text()
checks['Actual denied access is audited'] = 'TICKET_ACCESS_DENIED' in code and 'DOWNLOAD_DENIED' in code
checks['30 documented test cases'] = len(re.findall(r'^\| TC\d\d \|', (root / 'tests/TEST_CASES.md').read_text(), re.M)) == 30
checks['Original demo is Development-only'] = 'app.Environment.IsDevelopment()' in (root / 'Program.cs').read_text()
checks['Role revalidation uses database'] = 'OnValidatePrincipal' in (root / 'Program.cs').read_text()
checks['M12 HTTPS launch profile'] = 'https://localhost:50900' in (root / 'Properties/launchSettings.json').read_text()
print('M12 v2 – static acceptance report (not a compilation or runtime test)')
for title, passed in checks.items():
    print(f"{'PASS' if passed else 'FAIL'}  {title}")
print(f'STATIC: {sum(checks.values())}/{len(checks)} checks. RUNTIME/SQL: NOT TESTED.')
if not all(checks.values()): raise SystemExit(1)
