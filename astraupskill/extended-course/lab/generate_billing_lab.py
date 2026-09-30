"""Copy current MeterForge billing source into a fresh external experiment."""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    parser.add_argument('--mode', choices=['reference', 'starter'], default='reference')
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    root = here.parents[2]
    output = Path(args.output).expanduser()
    if not output.is_absolute():
        parser.error('Output must be a new absolute external directory.')
    output = output.resolve()
    if output.exists() or output == root or root in output.parents:
        parser.error('Existing and in-project outputs are refused.')
    paths = ['src/MeterForge.Domain/Common/BusinessRuleException.cs',
             'src/MeterForge.Domain/Common/Money.cs',
             'src/MeterForge.Domain/Invoicing/Invoice.cs',
             'src/MeterForge.Application/Persistence/InvoiceSnapshot.cs',
             'src/MeterForge.Domain/Payments/PaymentAttempt.cs',
             'src/MeterForge.Application/Payments/PaymentService.cs',
             'src/MeterForge.Domain/Entitlements/Entitlements.cs',
             'src/MeterForge.Domain/Adjustments/Adjustments.cs',
             'src/MeterForge.Domain/Reconciliation/Reconciliation.cs']
    prepared = []
    for relative in paths:
        raw = (root / relative).read_bytes()
        copied = raw
        mutation = None
        if args.mode == 'starter' and relative.endswith('/Adjustments.cs'):
            source = raw.decode('utf-8-sig')
            anchor = 'amount.Amount * Percent / 100m'
            if source.count(anchor) != 1:
                raise ValueError('Discount expression changed; review starter mutation.')
            copied = source.replace(anchor, 'amount.Amount * (100m - Percent) / 100m').encode('utf-8')
            mutation = 'Deliberately return net amount where reduction is required.'
        prepared.append((relative, raw, copied, mutation))
    program = (here / 'BillingProgram.cs.txt').read_text(encoding='utf-8')
    output.mkdir(parents=True, exist_ok=False)
    manifest = []
    for relative, raw, copied, mutation in prepared:
        target = output / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(copied)
        manifest.append({'path': relative, 'source_sha256': hashlib.sha256(raw).hexdigest(),
                         'copied_sha256': hashlib.sha256(copied).hexdigest(), 'mutation': mutation})
    (output / 'Program.cs').write_text(program, encoding='utf-8')
    (output / 'Lab.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>\n', encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>\n', encoding='utf-8')
    (output / 'SOURCE-MAP.json').write_text(json.dumps({'mode': args.mode, 'files': manifest}, indent=2)+'\n', encoding='utf-8')
    print(output)


if __name__ == '__main__':
    main()
