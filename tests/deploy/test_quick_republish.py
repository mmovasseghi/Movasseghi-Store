"""Fault-injection tests. All privileged commands are replaced by local fixtures."""
from pathlib import Path
import os
import shutil
import sqlite3
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


class DeployTests(unittest.TestCase):
    def run_deploy(self, scenario):
        self.temporary = tempfile.TemporaryDirectory(prefix='movasseghi-deploy-test-')
        self.addCleanup(self.temporary.cleanup)
        root = Path(self.temporary.name)
        app = root / 'app'
        source = app / 'src'
        live = app / 'publish'
        config = root / 'nginx'
        for p in [source / 'deploy/linux', live / 'wwwroot/images', live / 'App_Data', config / 'sites-available', config / 'sites-enabled', config / 'conf.d', root / 'bin']:
            p.mkdir(parents=True, exist_ok=True)
        (live / 'RELEASE_COMMIT').write_text('old')
        (live / 'wwwroot/images/upload.webp').write_bytes(b'original upload')
        (live / 'App_Data/persistent.json').write_text('{"keep":true}')
        (live / 'appsettings.Production.json').write_text('{"keep":true}')
        with sqlite3.connect(live / 'movasseghi.db') as db:
            db.execute('create table Orders (id integer primary key, note text)')
            db.execute("insert into Orders values (1, 'existing order')")
        for name in ['sites-available/movasseghi-shop', 'conf.d/ms-cache.conf']:
            (config / name).write_text('old config')
        (config / 'sites-enabled/movasseghi-shop').symlink_to(config / 'sites-available/movasseghi-shop')
        (source / 'deploy/linux/nginx-movasseghi.conf').write_text('new config')
        (source / 'deploy/linux/verify-home.py').write_text("import os,sys\nprint('probe',sys.argv[1])\ncase=os.environ['SCENARIO']\nraise SystemExit(1 if case=='smoke-failure' or (case=='proxy-smoke-failure' and sys.argv[1]=='http://127.0.0.1') else 0)\n")
        fixture = '''#!/usr/bin/env python3
from pathlib import Path
import os,sys
name=Path(sys.argv[0]).name
app=Path(os.environ['FIXTURE_APP']); args=sys.argv[1:]; case=os.environ['SCENARIO']
with (app/'calls').open('a') as log: log.write(name+' '+ ' '.join(args)+'\\n')
if name=='id': print('0')
elif name=='ss': print('LISTEN 0 512 127.0.0.1:5080 0.0.0.0:*')
elif name=='git':
 if 'pull' in args and case=='pull-failure': sys.exit(1)
 if 'rev-parse' in args: print('new')
elif name=='npm':
 if case=='build-failure' and 'build' in args: sys.exit(1)
elif name=='dotnet':
 dest=Path(args[args.index('-o')+1]); (dest/'wwwroot/images').mkdir(parents=True); (dest/'App_Data').mkdir()
 binary=dest/'MovasseghiShop.Web'
 binary.write_text('#!/bin/sh\\ntest "$SCENARIO" != optimizer-failure\\n')
 binary.chmod(0o755)
elif name=='systemctl':
 if 'show' in args: print(str(app/'publish'))
 if 'start' in args and (case=='rollback-failure' or (case=='start-failure' and (app/'publish/RELEASE_COMMIT').read_text().strip()=='new')): sys.exit(1)
elif name=='curl': print('200',end='')
elif name=='nginx':
 flag=app/'nginx-failed-once'
 if case=='nginx-failure' and not flag.exists(): flag.touch(); sys.exit(1)
'''
        for name in ['id', 'ss', 'git', 'npm', 'dotnet', 'systemctl', 'curl', 'nginx', 'chown', 'sleep']:
            p = root / 'bin' / name
            p.write_text(fixture)
            p.chmod(0o755)
        script = (ROOT / 'deploy/linux/quick-republish.sh').read_text()
        script = script.replace('/MOVASSEGHISTORE', str(app)).replace('/etc/nginx', str(config)).replace('/var/cache/nginx', str(root / 'cache'))
        script = script.replace('export PATH="/usr/share/dotnet:/usr/local/bin:/usr/bin:/bin:$PATH"', '')
        target = root / 'deploy.sh'
        target.write_text(script)
        if scenario == 'existing-maintenance':
            (app / '.deploying').write_text('owned by another operation')
        environment = dict(os.environ, FIXTURE_APP=str(app), SCENARIO=scenario, PATH=str(root / 'bin') + os.pathsep + os.environ['PATH'])
        result = subprocess.run(['bash', str(target)], env=environment, text=True, capture_output=True, timeout=20)
        return app, config, result

    def assert_persistent(self, app):
        live = app / 'publish'
        self.assertEqual((live / 'wwwroot/images/upload.webp').read_bytes(), b'original upload')
        self.assertEqual((live / 'App_Data/persistent.json').read_text(), '{"keep":true}')
        self.assertEqual((live / 'appsettings.Production.json').read_text(), '{"keep":true}')
        with sqlite3.connect(live / 'movasseghi.db') as db:
            self.assertEqual(db.execute('select note from Orders where id=1').fetchone()[0], 'existing order')

    def test_success_preserves_database_config_uploads_and_records_sha(self):
        app, _, result = self.run_deploy('success')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((app / 'publish/RELEASE_COMMIT').read_text().strip(), 'new')
        self.assertFalse((app / '.deploying').exists())
        self.assert_persistent(app)
        self.assertTrue(list(app.glob('backups-release.*/previous-publish')))

    def test_failures_restore_previous_release_and_nginx(self):
        for scenario in ['pull-failure', 'build-failure', 'nginx-failure', 'optimizer-failure', 'start-failure', 'smoke-failure', 'proxy-smoke-failure']:
            with self.subTest(scenario=scenario):
                app, config, result = self.run_deploy(scenario)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual((app / 'publish/RELEASE_COMMIT').read_text().strip(), 'old', result.stderr)
                self.assertEqual((config / 'sites-available/movasseghi-shop').read_text(), 'old config')
                self.assertFalse((app / '.deploying').exists(), result.stderr)
                self.assert_persistent(app)

    def test_preexisting_maintenance_is_not_removed(self):
        app, _, result = self.run_deploy('existing-maintenance')
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual((app / '.deploying').read_text(), 'owned by another operation')

    def test_failed_recovery_keeps_maintenance_in_place(self):
        app, config, result = self.run_deploy('rollback-failure')
        self.assertNotEqual(result.returncode, 0)
        self.assertTrue((app / '.deploying').exists())
        self.assertEqual((config / 'sites-available/movasseghi-shop').read_text(), 'new config')
        self.assert_persistent(app)


if __name__ == '__main__':
    unittest.main()
