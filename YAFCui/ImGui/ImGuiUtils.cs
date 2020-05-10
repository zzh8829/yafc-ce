using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

namespace YAFC.UI
{
    public static class ImGuiUtils
    {
        public static readonly Padding DefaultButtonPadding = new Padding(1f, 0.5f);
        public static readonly Padding DefaultScreenPadding = new Padding(5f, 2f);
        public static readonly Padding DefaultIconPadding = new Padding(0.3f);
        
        public enum Event
        {
            None,
            Click,
            MouseOver,
            MouseDown,
        }

        public static Event BuildButton(this ImGui gui, Rect rect, SchemeColor normal, SchemeColor over, SchemeColor down = SchemeColor.None, uint button = SDL.SDL_BUTTON_LEFT)
        {
            switch (gui.action)
            {
                case ImGuiAction.MouseMove:
                    var wasOver = gui.IsMouseOver(rect);
                    return gui.ConsumeMouseOver(rect, RenderingUtils.cursorHand) && !wasOver ? Event.MouseOver : Event.None;
                case ImGuiAction.MouseDown:
                    return gui.actionParameter == button && gui.ConsumeMouseDown(rect, button) ? Event.MouseDown : Event.None;
                case ImGuiAction.MouseUp:
                    return gui.actionParameter == button && gui.ConsumeMouseUp(rect, true, button) ? Event.Click : Event.None;
                case ImGuiAction.Build:
                    var color = gui.IsMouseOver(rect) ? (down != SchemeColor.None && gui.IsMouseDown(rect, button)) ? down : over : normal;
                    gui.DrawRectangle(rect, color);
                    return Event.None;
                default:
                    return Event.None;
            }
        }
        
        public static bool BuildLink(this ImGui gui, string text)
        {
            gui.BuildText(text, color:SchemeColor.Link);
            var rect = gui.lastRect;
            switch (gui.action)
            {
                case ImGuiAction.MouseMove:
                    gui.ConsumeMouseOver(rect, RenderingUtils.cursorHand);
                    break;
                case ImGuiAction.MouseDown:
                    if (gui.actionParameter == SDL.SDL_BUTTON_LEFT)
                        gui.ConsumeMouseDown(rect);
                    break;
                case ImGuiAction.MouseUp:
                    if (gui.ConsumeMouseUp(rect))
                        return true;
                    break;
                case ImGuiAction.Build:
                    if (gui.IsMouseOver(rect))
                        gui.DrawRectangle(new Rect(rect.X, rect.Bottom-0.2f, rect.Width, 0.1f), SchemeColor.Link);
                    break;
            }

            return false;
        }

        public static bool BuildButtonClick(this ImGui gui, Rect rect, uint button = SDL.SDL_BUTTON_LEFT)
        {
            if (gui.actionParameter == button)
            {
                if (gui.action == ImGuiAction.MouseDown)
                    gui.ConsumeMouseDown(rect);
                else if (gui.action == ImGuiAction.MouseUp)
                    return gui.ConsumeMouseUp(rect);
            }

            return false;
        }

        public static bool OnClick(this ImGui gui, Rect rect)
        {
            if (gui.action == ImGuiAction.MouseUp)
                return gui.ConsumeMouseUp(rect);
            if (gui.action == ImGuiAction.MouseDown && gui.actionParameter == SDL.SDL_BUTTON_LEFT)
                gui.ConsumeMouseDown(rect);
            return false;
        }
        
        public static bool BuildButton(this ImGui gui, string text, SchemeColor color = SchemeColor.Primary, Padding? padding = null, bool active = true)
        {
            if (!active)
                color = SchemeColor.Grey;
            using (gui.EnterGroup(padding ?? DefaultButtonPadding, active ? color+2 : color+3))
                gui.BuildText(text, Font.text, align:RectAlignment.Middle);

            return gui.BuildButton(gui.lastRect, color, color + 1) == Event.Click && active;
        }

        public static Event BuildRedButton(this ImGui gui, string text)
        {
            Rect textRect;
            TextCache cache;
            using (gui.EnterGroup(DefaultButtonPadding))
                textRect = gui.AllocateTextRect(out cache, text, align:RectAlignment.Middle);
            var evt = gui.BuildButton(gui.lastRect, SchemeColor.None, SchemeColor.Error);
            if (gui.isBuilding)
                gui.DrawRenderable(textRect, cache, gui.IsMouseOver(gui.lastRect) ? SchemeColor.ErrorText : SchemeColor.Error);
            return evt;
        }
        
        public static Event BuildRedButton(this ImGui gui, Icon icon, float size = 1.5f)
        {
            Rect iconRect;
            using (gui.EnterGroup(new Padding(0.3f)))
                iconRect = gui.AllocateRect(size, size, RectAlignment.Middle);
            var evt = gui.BuildButton(gui.lastRect, SchemeColor.None, SchemeColor.Error);
            if (gui.isBuilding)
                gui.DrawIcon(iconRect, icon, gui.IsMouseOver(gui.lastRect) ? SchemeColor.ErrorText : SchemeColor.Error);
            return evt;
        }

        public static bool BuildButton(this ImGui gui, Icon icon, SchemeColor normal = SchemeColor.None, SchemeColor over = SchemeColor.Grey, SchemeColor down = SchemeColor.None, float size = 1.5f)
        {
            using (gui.EnterGroup(new Padding(0.3f)))
                gui.BuildIcon(icon, size);
            return gui.BuildButton(gui.lastRect, normal, over, down) == Event.Click;
        }

        public static bool BuildCheckBox(this ImGui gui, string text, bool value, out bool newValue, SchemeColor color = SchemeColor.None)
        {
            using (gui.EnterRow())
            {
                gui.BuildIcon(value ? Icon.CheckBoxCheck : Icon.CheckBoxEmpty, 1.5f, color);
                gui.BuildText(text, Font.text, color:color);
            }

            if (gui.OnClick(gui.lastRect))
            {
                newValue = !value;
                return true;
            }

            newValue = value;
            return false;
        }

        public static bool BuildRadioGroup(this ImGui gui, IReadOnlyList<string> options, int selected, out int newSelected, SchemeColor color = SchemeColor.None)
        {
            newSelected = selected;
            for (var i = 0; i < options.Count; i++)
            {
                using (gui.EnterRow())
                {
                    gui.BuildIcon(selected == i ? Icon.RadioCheck : Icon.RadioEmpty, 1.5f, color);
                    gui.BuildText(options[i], Font.text, color:color);
                }
                
                if (gui.OnClick(gui.lastRect))
                    newSelected = i;
            }

            return newSelected != selected;
        }

        public static void ShowDropDown(this ImGui gui, Rect rect, SimpleDropDown.Builder builder, float width = 20f) => gui.window?.ShowDropDown(gui, rect, builder, width);
        public static void ShowDropDown(this ImGui gui, SimpleDropDown.Builder builder, float width = 20f) => gui.window?.ShowDropDown(gui, gui.lastRect, builder, width);
        public static void ShowTooltip(this ImGui gui, Rect rect, Action<ImGui> builder, float width = 20f) => gui.window?.ShowTooltip(gui, rect, builder, width);
        public static void ShowTooltip(this ImGui gui, Action<ImGui> builder, float width = 20f) => gui.window?.ShowTooltip(gui, gui.lastRect, builder, width);
        
        public struct InlineGridBuilder : IDisposable
        {
            private ImGui.Context savedContext;
            private readonly ImGui gui;
            private readonly int elementsPerRow;
            private readonly float elementWidth;
            private int currentRowIndex;

            internal InlineGridBuilder(ImGui gui, float elementWidth, int elementsPerRow)
            {
                savedContext = default;
                this.gui = gui;
                gui.allocator = RectAllocator.LeftAlign;
                gui.spacing = 0f;
                this.elementWidth = MathF.Min(elementWidth, gui.width);
                this.elementsPerRow = elementsPerRow == 0 ? MathUtils.Floor(gui.width / elementWidth) : elementsPerRow;
                currentRowIndex = -1;
                if (elementWidth <= 0)
                    this.elementsPerRow = 1;
            }

            public void Next()
            {
                if (currentRowIndex == elementsPerRow-1)
                {
                    savedContext.Dispose();
                    savedContext = default;
                    currentRowIndex = -1;
                }
                currentRowIndex++;
                if (currentRowIndex == 0)
                {
                    savedContext = gui.EnterRow(0f);
                    gui.allocator = RectAllocator.Stretch;
                }
                savedContext.SetManualRect(new Rect(elementWidth * currentRowIndex, 0f, elementWidth, 0f));
            }

            public void Dispose()
            {
                savedContext.Dispose();
            }
        }

        public static InlineGridBuilder EnterInlineGrid(this ImGui gui, float elementWidth, int maxElemCount = 0)
        {
            return new InlineGridBuilder(gui, elementWidth, maxElemCount);
        }

        public static bool DoListReordering<T>(this ImGui gui, Rect moveHandle, Rect contents, T index, out T moveFrom, SchemeColor backgroundColor = SchemeColor.PureBackground, bool updateDraggingObject = true)
        {
            var result = false;
            moveFrom = index;
            if (!gui.InitiateDrag(moveHandle, contents, index, backgroundColor) && gui.action == ImGuiAction.MouseDrag && gui.ConsumeDrag(contents.Center, index))
            {
                moveFrom = gui.GetDraggingObject<T>(); 
                if (updateDraggingObject)
                    gui.UpdateDraggingObject(index);
                result = true;
            }
            return result;
        }

        public static bool InitiateDrag<T>(this ImGui gui, Rect moveHandle, Rect contents, T index, SchemeColor backgroundColor = SchemeColor.PureBackground)
        {
            if ((gui.action == ImGuiAction.MouseDown && gui.ConsumeMouseDown(moveHandle)) || (gui.action == ImGuiAction.Build && gui.IsDragging(index)))
            {
                gui.SetDraggingArea(contents, index, backgroundColor);
                return true;
            }
            return false;
        }
    }
}