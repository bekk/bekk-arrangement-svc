export const hasChristmasSpirit = (eventTitle: string) =>
  ['🎅', '🧑‍🎄', '🤶', '🎄', 'christmas'].some((emoji) =>
    eventTitle.toLowerCase().includes(emoji)
  );

export const hasHalloweenSpirit = (eventTitle: string) =>
  ['🎃', '👻', '🦇', '🧛', '🧛‍♀️', '🧛‍♂️', 'halloween'].some((emoji) =>
    eventTitle.toLowerCase().includes(emoji)
  );

export const hasKittens = (eventTitle: string) =>
  ['🐱', '😻', '🌈', '🦄', '🐈', '🏳️‍🌈'].some((emoji) =>
    eventTitle.toLowerCase().includes(emoji)
  );
