import bpy

# 创建一个 Cube（Box）
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, 0))

# 弹窗提示“成功”
def show_success():
    bpy.context.window_manager.popup_menu(
        lambda self, ctx: self.layout.label(text="ZW_Tool 测试成功！已创建 Cube"),
        title="成功",
        icon='INFO'
    )

# 延迟一帧执行弹窗（避免上下文问题）
bpy.app.timers.register(show_success, first_interval=0.1)

print("=== ZW_Tool 测试脚本执行完成：Cube 已创建 ===")