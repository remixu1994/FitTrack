using System.Text.Json.Serialization;

namespace FitTrack.Copilot.AI.RAG;

public class FitnessKnowledge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // exercise, nutrition, plan
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetMuscle { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Equipment { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string CommonMistakes { get; set; } = string.Empty;
}

public class FitnessKnowledgeBase
{
    private readonly List<FitnessKnowledge> _knowledge;

    public FitnessKnowledgeBase()
    {
        _knowledge = InitializeKnowledgeBase();
    }

    public IReadOnlyList<FitnessKnowledge> GetAll() => _knowledge.AsReadOnly();

    public List<FitnessKnowledge> Search(string query)
    {
        var queryLower = query.ToLower();
        return _knowledge.Where(k =>
            k.Name.ToLower().Contains(queryLower) ||
            k.Description.ToLower().Contains(queryLower) ||
            k.TargetMuscle.ToLower().Contains(queryLower) ||
            k.Tags.Any(t => t.ToLower().Contains(queryLower)) ||
            k.Equipment.ToLower().Contains(queryLower)
        ).ToList();
    }

    public List<FitnessKnowledge> GetByMuscleGroup(string muscleGroup)
    {
        return _knowledge.Where(k => 
            k.TargetMuscle.ToLower().Contains(muscleGroup.ToLower())
        ).ToList();
    }

    public List<FitnessKnowledge> GetByDifficulty(string difficulty)
    {
        return _knowledge.Where(k => 
            k.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    private List<FitnessKnowledge> InitializeKnowledgeBase()
    {
        return new List<FitnessKnowledge>
        {
            // CHEST EXERCISES
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Bench Press",
                Description = "A compound exercise that targets the chest, shoulders, and triceps.",
                TargetMuscle = "Chest",
                Difficulty = "Intermediate",
                Tags = new List<string> { "chest", "compound", "barbell", "strength" },
                Equipment = "Barbell, Bench",
                Instructions = "1. Lie on bench with eyes under the bar\n2. Grip slightly wider than shoulder-width\n3. Plant feet firmly on ground\n4. Lower bar to mid-chest with control\n5. Press bar up in slight arc\n6. Lock out at top without flaring elbows",
                CommonMistakes = "Bouncing bar off chest, flaring elbows too wide, lifting hips off bench"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Dumbbell Press",
                Description = "A compound exercise for chest using dumbbells.",
                TargetMuscle = "Chest",
                Difficulty = "Beginner",
                Tags = new List<string> { "chest", "compound", "dumbbell" },
                Equipment = "Dumbbells, Bench",
                Instructions = "1. Lie on bench holding dumbbells\n2. Start with arms extended above chest\n3. Lower dumbbells to chest level\n4. Press back up to starting position\n5. Keep slight bend in elbows",
                CommonMistakes = "Using too heavy weight, bouncing dumbbells"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Push-Up",
                Description = "A classic bodyweight exercise for chest, shoulders, and triceps.",
                TargetMuscle = "Chest",
                Difficulty = "Beginner",
                Tags = new List<string> { "chest", "bodyweight", "compound", "home" },
                Equipment = "None",
                Instructions = "1. Start in plank position\n2. Hands slightly wider than shoulders\n3. Lower body until chest nearly touches floor\n4. Keep core tight\n5. Push back up to starting position\n6. Keep body in straight line throughout",
                CommonMistakes = "Sagging hips, flaring elbows, incomplete range of motion"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Dumbbell Fly",
                Description = "An isolation exercise targeting the chest muscles.",
                TargetMuscle = "Chest",
                Difficulty = "Intermediate",
                Tags = new List<string> { "chest", "isolation", "dumbbell" },
                Equipment = "Dumbbells, Bench",
                Instructions = "1. Lie on bench holding dumbbells\n2. Extend arms above chest with palms facing\n3. Lower dumbbells in arc motion\n4. Feel stretch in chest\n5. Return to starting position\n6. Keep slight bend in elbows throughout",
                CommonMistakes = "Using too much weight, dropping shoulders, incomplete stretch"
            },

            // BACK EXERCISES
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Deadlift",
                Description = "A fundamental compound movement targeting the entire posterior chain.",
                TargetMuscle = "Back",
                Difficulty = "Advanced",
                Tags = new List<string> { "back", "compound", "barbell", "strength", "legs" },
                Equipment = "Barbell",
                Instructions = "1. Stand with feet hip-width apart, bar over mid-foot\n2. Hinge at hips and grip bar just outside legs\n3. Keep back flat, chest up\n4. Engage lats and brace core\n5. Drive through heels, extend hips and knees together\n6. Stand tall, squeeze glutes at top\n7. Return bar to floor with control",
                CommonMistakes = "Rounding lower back, jerking the bar, hyperextending at top"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Pull-Up",
                Description = "A bodyweight exercise for building back and bicep strength.",
                TargetMuscle = "Back",
                Difficulty = "Intermediate",
                Tags = new List<string> { "back", "bodyweight", "compound", "pull" },
                Equipment = "Pull-up Bar",
                Instructions = "1. Hang from bar with overhand grip, slightly wider than shoulders\n2. Engage lats and pull shoulder blades down\n3. Pull yourself up until chin clears the bar\n4. Lower with control to full arm extension\n5. Keep core tight throughout",
                CommonMistakes = "Using momentum, incomplete range of motion, shrugging shoulders"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Barbell Row",
                Description = "A compound exercise for building back thickness.",
                TargetMuscle = "Back",
                Difficulty = "Intermediate",
                Tags = new List<string> { "back", "compound", "barbell", "strength" },
                Equipment = "Barbell",
                Instructions = "1. Stand with feet shoulder-width apart\n2. Hinge forward at hips, keep back flat\n3. Grip bar slightly wider than shoulders\n4. Pull bar to lower chest/upper abdomen\n5. Squeeze shoulder blades together\n6. Lower with control",
                CommonMistakes = "Using too much body English, not pulling to correct position, rounded back"
            },

            // LEG EXERCISES
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Squat",
                Description = "The king of leg exercises, targeting quads, hamstrings, and glutes.",
                TargetMuscle = "Legs",
                Difficulty = "Intermediate",
                Tags = new List<string> { "legs", "compound", "barbell", "strength", "squats" },
                Equipment = "Barbell, Squat Rack",
                Instructions = "1. Position bar on upper back\n2. Stand with feet shoulder-width apart\n3. Keep chest up and core engaged\n4. Lower body as if sitting into a chair\n5. Keep knees in line with toes\n6. Go down until thighs are parallel to ground\n7. Drive through heels to stand back up",
                CommonMistakes = "Knees caving inward, rounding lower back, heels lifting off ground"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Lunges",
                Description = "A unilateral leg exercise for strength and balance.",
                TargetMuscle = "Legs",
                Difficulty = "Beginner",
                Tags = new List<string> { "legs", "compound", "bodyweight", "balance" },
                Equipment = "None or Dumbbells",
                Instructions = "1. Stand with feet hip-width apart\n2. Step forward with one leg\n3. Lower until both knees are at 90 degrees\n4. Keep front knee over ankle, not past toes\n5. Push through front heel to return\n6. Alternate legs",
                CommonMistakes = "Front knee extending past toes, taking too short steps, leaning forward"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Romanian Deadlift",
                Description = "An exercise targeting the hamstrings and glutes.",
                TargetMuscle = "Hamstrings",
                Difficulty = "Intermediate",
                Tags = new List<string> { "legs", "compound", "barbell", "hamstrings" },
                Equipment = "Barbell",
                Instructions = "1. Stand with feet hip-width apart, bar in front of thighs\n2. Slight bend in knees\n3. Hinge at hips, pushing them back\n4. Lower bar along legs\n5. Feel stretch in hamstrings\n6. Drive hips forward to return",
                CommonMistakes = "Too much knee bend, rounding back, not feeling hamstring stretch"
            },

            // SHOULDER EXERCISES
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Overhead Press",
                Description = "A compound movement for shoulder development.",
                TargetMuscle = "Shoulders",
                Difficulty = "Intermediate",
                Tags = new List<string> { "shoulders", "compound", "barbell", "strength" },
                Equipment = "Barbell",
                Instructions = "1. Stand with feet shoulder-width apart\n2. Grip bar slightly wider than shoulders\n3. Position bar at collar bone\n4. Press bar overhead in slight arc\n5. Lock out arms at top\n6. Lower with control",
                CommonMistakes = "Excessive back arch, not pressing straight up, incomplete range of motion"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Lateral Raise",
                Description = "An isolation exercise for the lateral deltoids.",
                TargetMuscle = "Shoulders",
                Difficulty = "Beginner",
                Tags = new List<string> { "shoulders", "isolation", "dumbbell" },
                Equipment = "Dumbbells",
                Instructions = "1. Stand with dumbbells at sides\n2. Keep slight bend in elbows\n3. Raise arms out to sides\n4. Stop at shoulder height\n5. Lower with control\n6. Keep core tight throughout",
                CommonMistakes = "Using momentum, going too heavy, shrugging shoulders"
            },

            // CORE EXERCISES
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Plank",
                Description = "An isometric core exercise for stability and endurance.",
                TargetMuscle = "Core",
                Difficulty = "Beginner",
                Tags = new List<string> { "core", "bodyweight", "isometric", "home" },
                Equipment = "None",
                Instructions = "1. Start in push-up position on forearms\n2. Keep body in straight line from head to heels\n3. Engage core and squeeze glutes\n4. Don't let hips sag or pike up\n5. Hold position while breathing normally",
                CommonMistakes = "Hips too high, hips too low, holding breath"
            },
            new FitnessKnowledge
            {
                Type = "exercise",
                Name = "Hanging Leg Raise",
                Description = "An advanced core exercise targeting the lower abs.",
                TargetMuscle = "Core",
                Difficulty = "Advanced",
                Tags = new List<string> { "core", "bodyweight", "advanced" },
                Equipment = "Pull-up Bar",
                Instructions = "1. Hang from pull-up bar\n2. Keep legs straight or bent\n3. Raise legs to parallel or higher\n4. Control the descent\n5. Avoid swinging",
                CommonMistakes = "Using momentum, swinging legs, not engaging core"
            },

            // NUTRITION KNOWLEDGE
            new FitnessKnowledge
            {
                Type = "nutrition",
                Name = "Protein Requirements",
                Description = "Guidelines for daily protein intake based on goals.",
                TargetMuscle = "General",
                Difficulty = "Beginner",
                Tags = new List<string> { "nutrition", "protein", "diet" },
                Equipment = "N/A",
                Instructions = @"General protein guidelines:

• Sedentary adults: 0.8g per kg body weight
• Active individuals: 1.2-1.7g per kg body weight
• Muscle building: 1.6-2.2g per kg body weight
• Fat loss while preserving muscle: 1.6-2.4g per kg body weight

Best protein sources:
• Chicken breast, turkey
• Fish (salmon, tuna, cod)
• Eggs
• Greek yogurt
• Lean beef
• Tofu, tempeh
• Legumes",
                CommonMistakes = "Not eating enough protein, over-consuming protein, ignoring overall diet quality"
            },
            new FitnessKnowledge
            {
                Type = "nutrition",
                Name = "Pre-Workout Nutrition",
                Description = "What to eat before exercising for optimal performance.",
                TargetMuscle = "General",
                Difficulty = "Beginner",
                Tags = new List<string> { "nutrition", "pre-workout", "energy" },
                Equipment = "N/A",
                Instructions = @"Pre-workout meal guidelines:

2-3 hours before:
• Complete meal with protein, carbs, and fat
• Example: Chicken breast, rice, vegetables

30-60 minutes before:
• Light snack, mainly carbs
• Example: Banana, toast with peanut butter, sports drink

Avoid:
• High fat foods (slow digestion)
• High fiber foods (gas, discomfort)
• New/strange foods

Stay hydrated!",
                CommonMistakes = "Eating too close to workout, eating high fat meals, not staying hydrated"
            },
            new FitnessKnowledge
            {
                Type = "nutrition",
                Name = "Post-Workout Nutrition",
                Description = "Recovery nutrition for optimal muscle repair.",
                TargetMuscle = "General",
                Difficulty = "Beginner",
                Tags = new List<string> { "nutrition", "post-workout", "recovery", "protein" },
                Equipment = "N/A",
                Instructions = @"Post-workout nutrition guidelines:

The anabolic window myth is debunked - timing matters less than total daily intake.

Key principles:
• Protein: 20-40g within a few hours
• Carbs: Replenish glycogen stores
• Fluids: Rehydrate

Good post-workout options:
• Protein shake with banana
• Chicken with rice and vegetables
• Greek yogurt with berries
• Eggs on whole grain toast

Pair protein with carbs for best recovery!",
                CommonMistakes = "Obsessing over timing, skipping post-workout meal entirely, over-consuming calories"
            },

            // FITNESS PLANS
            new FitnessKnowledge
            {
                Type = "plan",
                Name = "Beginner Strength Program",
                Description = "A 4-week program for those new to strength training.",
                TargetMuscle = "Full Body",
                Difficulty = "Beginner",
                Tags = new List<string> { "plan", "beginner", "strength", "4-week" },
                Equipment = "Dumbbells or Barbell",
                Instructions = @"Week 1-2 (3 days per week):

Day A:
• Squats: 3x10
• Push-ups: 3x8-10
• Rows: 3x10
• Plank: 3x30 sec

Day B:
• Lunges: 3x10 each leg
• Dumbbell Press: 3x10
• Pull-ups or Rows: 3x8
• Core exercises

Day C:
• Rest or light cardio

Week 3-4:
• Increase weight by 5-10%
• Add 1-2 more reps per set

Focus on form over weight!",
                CommonMistakes = "Going too heavy, skipping rest days, not progressing"
            },
            new FitnessKnowledge
            {
                Type = "plan",
                Name = "Weight Loss Program",
                Description = "A comprehensive plan for fat loss and conditioning.",
                TargetMuscle = "Full Body",
                Difficulty = "Intermediate",
                Tags = new List<string> { "plan", "weight loss", "fat loss", "cardio" },
                Equipment = "Various",
                Instructions = @"Weekly Structure:

Monday: Upper Body Strength + Cardio
Tuesday: Lower Body Strength + Core
Wednesday: HIIT (20-30 min)
Thursday: Active Recovery (Yoga, Walking)
Friday: Full Body Strength
Saturday: Long Cardio (45-60 min)
Sunday: Rest

Nutrition:
• Create 300-500 calorie deficit
• High protein: 1.6-2g per kg body weight
• Complex carbs around workouts
• Plenty of vegetables

Key Tips:
• Track everything
• Sleep 7-9 hours
• Stay consistent",
                CommonMistakes = "Too much cardio, not enough strength training, extreme calorie restriction"
            }
        };
    }
}
