project(
    'VSInjector',
    'cpp',
    version: '0.1.1',
    default_options: ['cpp_std=c++20', 'default_library=static'],
)

pymod = import('python')
py3 = pymod.find_installation('python3')

proxy_gen = custom_target(
    'proxygen',
    input: 'proxygen.py',
    output: ['GEN_proxy.hpp', 'GEN_proxy.def'],
    command: [py3, '@INPUT@', '@OUTPUT0@', '@OUTPUT1@']
)

generated_proxy_hpp = proxy_gen[0]
generated_proxy_def = proxy_gen[1]

cpp = meson.get_compiler('cpp')
root_dir = meson.current_source_dir()

minhook_dep = dependency('minhook')
nethost_dep = dependency('nethost')

windows = import('windows')
dll_info = windows.compile_resources('dll_info.rc')

dll_source = [
    dll_info,
    generated_proxy_hpp,
    'main.cpp',
    'config.hpp',
    'proxy_loader.hpp',
    'proxy_exports.cpp',
    'dotnet_launcher.hpp',
    'dotnet_launcher.cpp',
]

proxy_dll = shared_library(
    'version',
    dll_source,
    dependencies: [nethost_dep, minhook_dep],
    link_args: ['/DEF:' + generated_proxy_def.full_path()],
)
