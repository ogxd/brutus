import os, subprocess, yaml, time, asyncio, sys, requests, urllib
from itertools import cycle

def alert(website):
  print(f"ALERT!!! -> {website['url']}")

proxies = ['51.159.5.133:3128']
proxy_pool = cycle(proxies)
proxy = next(proxy_pool)

#proxy_support = urllib.request.ProxyHandler({'http' : proxy, 'https': proxy})
#opener = urllib.request.build_opener(proxy_support)
#urllib.request.install_opener(opener)

async def scrape(website):
  while (True):
    content = ""
    if 'render' in website and website['render']:
      content += subprocess.run([
        "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
        "--headless",
        "--disable-gpu",
        "--dump-dom",
        website['url']], capture_output=True, text=True).stdout
    else:
      resource = urllib.request.urlopen(website['url'])
      content = resource.read().decode(resource.headers.get_content_charset())

    if 'excludes' in website and website['excludes'] not in content:
      alert(website)
      return

    if 'includes' in website and website['includes'] in content:
      alert(website)
      return

    await asyncio.sleep(5 if 'delay' not in website else website['delay'])

async def main():
  config = {}

  with open("config.yaml") as file:
    config = yaml.load(file, Loader=yaml.FullLoader)

  tasks = []

  for website in config['websites']:
    tasks.append(scrape(website))

  for task in tasks:
    await task

asyncio.run(main())