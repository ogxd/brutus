import os, subprocess, yaml, time

url_amazon = "https://www.amazon.fr/PlayStation-%C3%89dition-Standard-DualSense-Couleur/dp/B08H93ZRK9/ref=sr_1_1?__mk_fr_FR=%C3%85M%C3%85%C5%BD%C3%95%C3%91&keywords=ps5&qid=1637790104&sr=8-1"
command = "\"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome\" --headless --disable-gpu --dump-dom" #--print-to-pdf

#cake = subprocess.check_output(f"{command} {url_amazon}").read()

config = {}

with open("config.yaml") as file:
  config = yaml.load(file, Loader=yaml.FullLoader)

while (True):
  cake = subprocess.run([
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "--headless",
    "--disable-gpu",
    "--dump-dom",
    config['websites'][0]['url']], capture_output=True, text=True).stdout

  time.sleep(int(config['settings']['delay']))

  print(config['websites'][0]['excludes'] in cake)