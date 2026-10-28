"""只读提取截图相关文字证据，不反汇编，不执行目标，不复制程序代码。"""
from pathlib import Path
import hashlib
import json
import re
import struct

ROOT = Path(__file__).resolve().parent
TARGETS = [Path(r'D:\Tencent\Weixin\Weixin.exe'), Path(r'D:\Tencent\Weixin\4.1.15.56\Weixin.dll')]
ASCII_PATTERN = re.compile(rb'screenshot|screen_shot|screenshoter|we.?shot|long.?screen|mosaic|screenshotpinwindow', re.I)
ZH_TERMS = ['截图', '长截图', '马赛克', '贴图', '屏幕截图', '矩形', '提取文字', '识别文字']
SCREENSHOT_LABELS = ['提取文字、翻译等', '用 AI 处理', '提取文字', '更多...', '打马赛克', '消除杂物',
                     '翻译', '色值', '裁剪', '图片编辑', '退出', '按 Ctrl+C 复制色值',
                     '图片过大，暂不支持编辑', '滚动截图', '截图中断，返回中断位置继续滚动',
                     '已达最大长度，无法继续截图', '滚动页面截取更多内容', '坐标', '矩形',
                     '还原', '收藏', '一键打码', '箭头', '问 AI', '问 AI｜由元宝提供', '完成',
                     '编辑', '椭圆', '表情', '转发', '马赛克', '画笔', '置顶在桌面', '二维码',
                     '保存图片', '文字', '撤销']

def pe_layout(data):
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    machine, n_sections, _, _, _, optional_size, _ = struct.unpack_from('<HHIIIHH', data, pe+4)
    optional = pe+24
    magic = struct.unpack_from('<H', data, optional)[0]
    imagebase = struct.unpack_from('<Q' if magic == 0x20b else '<I', data, optional+(24 if magic == 0x20b else 28))[0]
    sections = []
    for n in range(n_sections):
        at = optional+optional_size+40*n
        name = data[at:at+8].split(b'\0')[0].decode('ascii', 'replace')
        vs, rva, size, offset = struct.unpack_from('<IIII', data, at+8)
        sections.append(dict(name=name, virtual_size=vs, rva=rva, raw_size=size, file_offset=offset))
    return dict(machine=hex(machine), optional_magic=hex(magic), image_base=hex(imagebase), sections=sections)

def location(layout, offset):
    for section in layout['sections']:
        if section['file_offset'] <= offset < section['file_offset']+section['raw_size']:
            return dict(section=section['name'], rva=hex(section['rva']+offset-section['file_offset']))
    return dict(section=None, rva=None)

def bounded_context(data, at, size, encoding):
    # UTF-8 稍多取一些字节，然后在 NUL、换行处截断；不推断其资源字段类型。
    low, high = max(0, at-90), min(len(data), at+size+160)
    if encoding == 'utf-8':
        low = max(low, data.rfind(b'\0', low, at)+1, data.rfind(b'\n', low, at)+1)
        for separator in [b'\0', b'\n']:
            end = data.find(separator, at+size, high)
            if end != -1:
                high = min(high, end)
    else:
        low = at  # UTF-16 只截取匹配词与后文，避免错误的双字节起始对齐。
        high -= (high-low) % 2
        for end in range(at+size, high, 2):
            if data[end:end+2] == b'\0\0':
                high = end
                break
    return data[low:high].decode(encoding, 'replace').replace('\r', ' ').replace('\n', ' ')

def analyze(path):
    data = path.read_bytes()
    layout = pe_layout(data)
    strings = []
    # 仅覆盖连续可打印 ASCII 字节，不是反编译器定义的字符串对象。
    for m in re.finditer(rb'[\x20-\x7e]{6,}', data):
        raw = m.group()
        if ASCII_PATTERN.search(raw):
            # 最大保留 600 个字符，避免将无关大段文字复制进报告。
            hit = ASCII_PATTERN.search(raw)
            start = max(0, hit.start()-100)
            text = raw[start:start+600].decode('ascii')
            strings.append(dict(file_offset=hex(m.start()+start), encoding='ascii', text=text,
                                truncated=(start != 0 or len(raw) > start+600), **location(layout, m.start()+start)))
    chinese = []
    counts = []
    for term in ZH_TERMS:
        for encoding in ['utf-8', 'utf-16le']:
            needle = term.encode(encoding)
            positions = []
            at = data.find(needle)
            while at != -1:
                positions.append(at)
                at = data.find(needle, at+len(needle))
            counts.append(dict(term=term, encoding=encoding, count=len(positions)))
            for at in positions[:30]:
                chinese.append(dict(term=term, file_offset=hex(at), encoding=encoding,
                                    context=bounded_context(data, at, len(needle), encoding),
                                    **location(layout, at)))
    labels = []
    if path.name == 'Weixin.dll':
        # 这是在初始截图词附近发现的一个连续中文文案区；边界固定，避免扩大到聊天内容。
        region_low, region_high = 0xb2e3300, 0xb2e35c0
        for label in SCREENSHOT_LABELS:
            at = data.find(b'\0'+label.encode('utf-8')+b'\0', region_low, region_high)
            if at != -1:
                at += 1
                labels.append(dict(text=label, file_offset=hex(at), encoding='utf-8', **location(layout, at)))
    return dict(path=str(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest(),
                scan_method='ordinary Python read-only byte/string scan; not REA decompilation',
                pe_layout=layout, ascii_match_count=len(strings), ascii_matches=strings,
                chinese_counts=counts, chinese_samples=chinese, screenshot_label_region=labels,
                limitations=['ASCII 连续可打印字节扫描与已列出的中文词精确编码扫描。',
                             '中文每词每编码最多 30 个样本；count 为整文件匹配数。',
                             '文件偏移是原文件位置；RVA 根据 PE 节表推导，不是调用地址。',
                             '字符串存在不能证明执行、调用关系、算法或功能可独立加载。',
                             '不搜索压缩/加密字节的明文，不断言未命中的词不存在于整个程序。'])

if __name__ == '__main__':
    results = [analyze(path) for path in TARGETS]
    output = ROOT/'static-screenshot-strings.json'
    output.write_text(json.dumps(dict(results=results), ensure_ascii=False, indent=2), encoding='utf-8')
    for result in results:
        print(result['path'], result['sha256'], 'ASCII', result['ascii_match_count'])
        print(json.dumps(result['chinese_counts'], ensure_ascii=False))
    print(output)
