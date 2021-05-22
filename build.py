import os
import sys
import glob
from zipfile import ZipFile

if len(sys.argv) == 2:
    version = int(sys.argv[1])
else:
    version = int(input('var version: '))

cslistName = 'ADD_ME.cslist'
varName = 'Yoooi.BusDriver.{}.var'.format(version)
zipPath = 'Custom\\Scripts\\Yoooi\\BusDriver\\'

print('Creating "{}"'.format(cslistName))
with open('ADD_ME.cslist', 'w+', encoding='utf-8') as cslist:
    for file in sorted(glob.glob('**/*.cs', recursive=True)):
        if not file.startswith('src') and not file.startswith('lib'):
            continue

        cslist.write('{}\n'.format(file))
        print('Adding "{}"'.format(file))

print('Creating "{}"'.format(varName))
with open(cslistName, 'r', encoding = 'utf-8') as cslist:
    with ZipFile(varName, 'w') as var:
        var.write('meta.json')
        var.write('LICENSE.md')
        var.write(cslistName, os.path.join(zipPath, cslistName))
        for file in [x.strip() for x in cslist]:
            var.write(file, os.path.join(zipPath, file))

        for file in var.namelist():
            print('Adding "{}"'.format(file))
