import json

with open(r'c:\Users\Asus\.gemini\antigravity-ide\brain\502b8d60-11d2-40fe-aeff-26c1c0ce311c\.system_generated\logs\transcript.jsonl', 'r', encoding='utf-8') as f:
    for line in f:
        if 'multi_replace_file_content' in line and 'Index.cshtml' in line:
            data = json.loads(line)
            if 'tool_calls' in data:
                for tc in data['tool_calls']:
                    if tc['name'] == 'multi_replace_file_content':
                        if 'TargetFile' in tc['args'] and 'Index.cshtml' in tc['args']['TargetFile']:
                            try:
                                args = tc['args']
                                chunks = json.loads(args['ReplacementChunks'])
                                # Find the chunk that replaced the step rails
                                for c in chunks:
                                    if 'step-bar' in c['TargetContent']:
                                        with open('old_rail.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                                    elif 'STEP 1' in c['TargetContent']:
                                        with open('old_step1.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                                    elif 'STEP 2' in c['TargetContent']:
                                        with open('old_step2.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                                    elif 'STEP 3' in c['TargetContent']:
                                        with open('old_step3.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                                    elif 'STEP 4' in c['TargetContent']:
                                        with open('old_step4.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                                    elif 'STEP 5' in c['TargetContent']:
                                        with open('old_step5.txt', 'w', encoding='utf-8') as out:
                                            out.write(c['TargetContent'])
                            except Exception as e:
                                pass
