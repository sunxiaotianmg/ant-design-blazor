---
category: Components
type: Layout
title: Flex
cols: 1
cover: https://mdn.alipayobjects.com/huamei_7uahnr/afts/img/A*SMzgSJZE_AwAAAAAAAAAAAAADrJ8AQ/original
coverDark: https://mdn.alipayobjects.com/huamei_7uahnr/afts/img/A*8yArQ43EGccAAAAAAAAAAAAADrJ8AQ/original
tag: New
---

Flex. Available since `0.17.0`.

## When To Use

- Good for setting spacing between elements.
- Suitable for setting various horizontal and vertical alignments.

### Difference with Space component

- Space is used to set the spacing between inline elements. It will add a wrapper element for each child element for inline alignment. Suitable for equidistant arrangement of multiple child elements in rows and columns.
- Flex is used to set the layout of block-level elements. It does not add a wrapper element. Suitable for layout of child elements in vertical or horizontal direction, and provides more flexibility and control.


## API

> This component is available since `0.17.0`. The default behavior of Flex in horizontal mode is to align upward, In vertical mode, aligns the stretch, You can adjust this via properties.

| Property  | Description                                                                | type                                                                    | Default              | Version |
|-----------|----------------------------------------------------------------------------|-------------------------------------------------------------------------|----------------------| --- |
| Vertical  | Obsolete, use `Direction = FlexDirection.Vertical`                          | boolean                                                                 | `false`              |  |
| Direction | Sets the direction of the flex, either horizontal or vertical              | FlexDirection                                                           | `false`              |  |
| Wrap      | Set whether the element is displayed in a single line or in multiple lines | FlexWrap                                                                | `FlexWrap.NoWrap`    |  |
| Justify   | Sets the alignment of elements in the direction of the main axis           | FlexJustify                                                             | `FlexJustify.Normal` |  |
| Align     | Sets the alignment of elements in the direction of the cross axis          | FlexAlign                                                               | `FlexAlign.Normal`    |  |
| Flex      | flex CSS shorthand properties                                              | reference [flex](https://developer.mozilla.org/en-US/docs/Web/CSS/flex) | normal               |  |
| Gap       | Sets the gap between grids                                                 | `small` \| `middle` \| `large` \| string \| number                      | -                    |  |

