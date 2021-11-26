import os, subprocess, yaml, time, asyncio

def alert(website):
  print(f"ALERT!!! -> {website['url']}")

async def scrape(website):
  while (True):
    content = subprocess.run([
      "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
      "--headless",
      "--disable-gpu",
      "--dump-dom",
      website['url']], capture_output=True, text=True).stdout

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