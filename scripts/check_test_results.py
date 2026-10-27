import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
if int(root.get('total', '0')) < 1 or root.get('result') != 'Passed' or int(root.get('failed', '0')):
    raise SystemExit('Unity tests did not pass or no tests ran')
print('Unity test results: passed')
